using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using ThermoCore.AWG.Calibration;
using ThermoCore.AWG.Configuration;
using ThermoCore.AWG.Simulation;

namespace ThermoCore.Persistence;

/// <summary>SQLite-backed ThermoCore store (DATA-004 MVP).</summary>
public sealed class SqliteThermoCoreStore : IThermoCoreStore, IDisposable
{
    private readonly string _connectionString;
    private bool _disposed;

    public SqliteThermoCoreStore(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        var directory = Path.GetDirectoryName(Path.GetFullPath(databasePath));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString();
    }

    public void EnsureCreated()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS configurations (
                id TEXT NOT NULL PRIMARY KEY,
                name TEXT NOT NULL,
                created_at_utc TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS configuration_versions (
                id TEXT NOT NULL PRIMARY KEY,
                configuration_id TEXT NOT NULL,
                version_number INTEGER NOT NULL,
                name TEXT NOT NULL,
                schema_version TEXT NOT NULL,
                configuration_json TEXT NOT NULL,
                content_hash TEXT NOT NULL,
                created_at_utc TEXT NOT NULL,
                UNIQUE(configuration_id, version_number)
            );

            CREATE TABLE IF NOT EXISTS simulation_summaries (
                id TEXT NOT NULL PRIMARY KEY,
                configuration_version_id TEXT NOT NULL,
                status TEXT NOT NULL,
                succeeded INTEGER NOT NULL,
                topology_id TEXT NOT NULL,
                completed_steps INTEGER NOT NULL,
                aggregated_energy_residual_j REAL NOT NULL,
                aggregated_water_residual_kg REAL NOT NULL,
                water_balance_passed INTEGER NOT NULL,
                energy_balance_passed INTEGER NOT NULL,
                final_water_tank_content_kg REAL NULL,
                created_at_utc TEXT NOT NULL,
                completed_at_utc TEXT NULL
            );

            CREATE TABLE IF NOT EXISTS calibration_runs (
                id TEXT NOT NULL PRIMARY KEY,
                measurement_source_path TEXT NOT NULL,
                baseline_configuration_version_id TEXT NULL,
                fitted_configuration_version_id TEXT NULL,
                algorithm TEXT NOT NULL,
                parameter_ids_json TEXT NOT NULL,
                initial_values_json TEXT NOT NULL,
                fitted_values_json TEXT NOT NULL,
                initial_objective REAL NOT NULL,
                final_objective REAL NOT NULL,
                evaluation_count INTEGER NOT NULL,
                created_at_utc TEXT NOT NULL
            );
            """;
        command.ExecuteNonQuery();
    }

    public StoredConfigurationVersion SaveConfiguration(
        AwgConfigurationDocument document,
        string name,
        string schemaVersion = "awg-v3-mvp-1")
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        EnsureCreated();

        var json = AwgConfigurationLoader.SaveToJson(document);
        var hash = ContentHasher.Sha256Hex(json);
        var configurationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using var connection = Open();
        using var tx = connection.BeginTransaction();

        using (var insertConfig = connection.CreateCommand())
        {
            insertConfig.Transaction = tx;
            insertConfig.CommandText =
                """
                INSERT INTO configurations (id, name, created_at_utc)
                VALUES ($id, $name, $created);
                """;
            insertConfig.Parameters.AddWithValue("$id", configurationId.ToString("N"));
            insertConfig.Parameters.AddWithValue("$name", name);
            insertConfig.Parameters.AddWithValue("$created", now.ToString("O", CultureInfo.InvariantCulture));
            insertConfig.ExecuteNonQuery();
        }

        using (var insertVersion = connection.CreateCommand())
        {
            insertVersion.Transaction = tx;
            insertVersion.CommandText =
                """
                INSERT INTO configuration_versions
                (id, configuration_id, version_number, name, schema_version, configuration_json, content_hash, created_at_utc)
                VALUES ($id, $configId, 1, $name, $schema, $json, $hash, $created);
                """;
            insertVersion.Parameters.AddWithValue("$id", versionId.ToString("N"));
            insertVersion.Parameters.AddWithValue("$configId", configurationId.ToString("N"));
            insertVersion.Parameters.AddWithValue("$name", name);
            insertVersion.Parameters.AddWithValue("$schema", schemaVersion);
            insertVersion.Parameters.AddWithValue("$json", json);
            insertVersion.Parameters.AddWithValue("$hash", hash);
            insertVersion.Parameters.AddWithValue("$created", now.ToString("O", CultureInfo.InvariantCulture));
            insertVersion.ExecuteNonQuery();
        }

        tx.Commit();

        return new StoredConfigurationVersion
        {
            Id = versionId,
            ConfigurationId = configurationId,
            VersionNumber = 1,
            Name = name,
            SchemaVersion = schemaVersion,
            ConfigurationJson = json,
            ContentHash = hash,
            CreatedAtUtc = now
        };
    }

    public StoredConfigurationVersion? GetConfigurationVersion(Guid id)
    {
        EnsureCreated();
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, configuration_id, version_number, name, schema_version, configuration_json, content_hash, created_at_utc
            FROM configuration_versions
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString("N"));
        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new StoredConfigurationVersion
        {
            Id = Guid.Parse(reader.GetString(0)),
            ConfigurationId = Guid.Parse(reader.GetString(1)),
            VersionNumber = reader.GetInt32(2),
            Name = reader.GetString(3),
            SchemaVersion = reader.GetString(4),
            ConfigurationJson = reader.GetString(5),
            ContentHash = reader.GetString(6),
            CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(7), CultureInfo.InvariantCulture)
        };
    }

    public StoredSimulationSummary SaveSimulationSummary(
        AwgSimulationRunResult run,
        Guid configurationVersionId)
    {
        ArgumentNullException.ThrowIfNull(run);
        EnsureCreated();

        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var summary = new StoredSimulationSummary
        {
            Id = id,
            ConfigurationVersionId = configurationVersionId,
            Status = run.EngineResult.Succeeded ? "Completed" : "Failed",
            Succeeded = run.Summary.Succeeded,
            TopologyId = run.Summary.TopologyId,
            CompletedSteps = run.Summary.CompletedSteps,
            AggregatedEnergyResidualJ = run.Summary.AggregatedEnergyResidualJ,
            AggregatedWaterResidualKg = run.Summary.AggregatedWaterResidualKg,
            WaterBalancePassed = run.BalanceReport.WaterBalancePassed,
            EnergyBalancePassed = run.BalanceReport.EnergyBalancePassed,
            FinalWaterTankContentKg = run.Summary.FinalWaterTankContentKg,
            CreatedAtUtc = now,
            CompletedAtUtc = now
        };

        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO simulation_summaries
            (id, configuration_version_id, status, succeeded, topology_id, completed_steps,
             aggregated_energy_residual_j, aggregated_water_residual_kg, water_balance_passed,
             energy_balance_passed, final_water_tank_content_kg, created_at_utc, completed_at_utc)
            VALUES
            ($id, $configVersionId, $status, $succeeded, $topologyId, $steps,
             $energyResidual, $waterResidual, $waterPass, $energyPass, $tank, $created, $completed);
            """;
        command.Parameters.AddWithValue("$id", summary.Id.ToString("N"));
        command.Parameters.AddWithValue("$configVersionId", configurationVersionId.ToString("N"));
        command.Parameters.AddWithValue("$status", summary.Status);
        command.Parameters.AddWithValue("$succeeded", summary.Succeeded ? 1 : 0);
        command.Parameters.AddWithValue("$topologyId", summary.TopologyId);
        command.Parameters.AddWithValue("$steps", summary.CompletedSteps);
        command.Parameters.AddWithValue("$energyResidual", summary.AggregatedEnergyResidualJ);
        command.Parameters.AddWithValue("$waterResidual", summary.AggregatedWaterResidualKg);
        command.Parameters.AddWithValue("$waterPass", summary.WaterBalancePassed ? 1 : 0);
        command.Parameters.AddWithValue("$energyPass", summary.EnergyBalancePassed ? 1 : 0);
        command.Parameters.AddWithValue(
            "$tank",
            summary.FinalWaterTankContentKg is { } tank ? tank : DBNull.Value);
        command.Parameters.AddWithValue("$created", summary.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue(
            "$completed",
            summary.CompletedAtUtc?.ToString("O", CultureInfo.InvariantCulture) ?? (object)DBNull.Value);
        command.ExecuteNonQuery();
        return summary;
    }

    public StoredCalibrationRun SaveCalibrationRun(
        AwgParameterCalibrationResult calibration,
        string measurementSourcePath,
        Guid? baselineConfigurationVersionId,
        Guid? fittedConfigurationVersionId)
    {
        ArgumentNullException.ThrowIfNull(calibration);
        ArgumentException.ThrowIfNullOrWhiteSpace(measurementSourcePath);
        EnsureCreated();

        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var parameterIds = calibration.Fitting.FittedValues.Keys.OrderBy(k => k, StringComparer.Ordinal).ToArray();
        var stored = new StoredCalibrationRun
        {
            Id = id,
            MeasurementSourcePath = measurementSourcePath,
            BaselineConfigurationVersionId = baselineConfigurationVersionId,
            FittedConfigurationVersionId = fittedConfigurationVersionId,
            Algorithm = "bounded-coordinate-descent-golden-section",
            ParameterIdsJson = JsonSerializer.Serialize(parameterIds),
            InitialValuesJson = JsonSerializer.Serialize(calibration.Fitting.InitialValues),
            FittedValuesJson = JsonSerializer.Serialize(calibration.Fitting.FittedValues),
            InitialObjective = calibration.Fitting.InitialObjective,
            FinalObjective = calibration.Fitting.FinalObjective,
            EvaluationCount = calibration.Fitting.EvaluationCount,
            CreatedAtUtc = now
        };

        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO calibration_runs
            (id, measurement_source_path, baseline_configuration_version_id, fitted_configuration_version_id,
             algorithm, parameter_ids_json, initial_values_json, fitted_values_json,
             initial_objective, final_objective, evaluation_count, created_at_utc)
            VALUES
            ($id, $source, $baseline, $fitted, $algorithm, $paramIds, $initial, $fittedValues,
             $initialObj, $finalObj, $evals, $created);
            """;
        command.Parameters.AddWithValue("$id", stored.Id.ToString("N"));
        command.Parameters.AddWithValue("$source", stored.MeasurementSourcePath);
        command.Parameters.AddWithValue(
            "$baseline",
            baselineConfigurationVersionId?.ToString("N") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue(
            "$fitted",
            fittedConfigurationVersionId?.ToString("N") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$algorithm", stored.Algorithm);
        command.Parameters.AddWithValue("$paramIds", stored.ParameterIdsJson);
        command.Parameters.AddWithValue("$initial", stored.InitialValuesJson);
        command.Parameters.AddWithValue("$fittedValues", stored.FittedValuesJson);
        command.Parameters.AddWithValue("$initialObj", stored.InitialObjective);
        command.Parameters.AddWithValue("$finalObj", stored.FinalObjective);
        command.Parameters.AddWithValue("$evals", stored.EvaluationCount);
        command.Parameters.AddWithValue("$created", stored.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
        command.ExecuteNonQuery();
        return stored;
    }

    public IReadOnlyList<StoredCalibrationRun> ListCalibrationRuns(int take = 50)
    {
        EnsureCreated();
        take = Math.Clamp(take, 1, 500);
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT id, measurement_source_path, baseline_configuration_version_id, fitted_configuration_version_id,
                   algorithm, parameter_ids_json, initial_values_json, fitted_values_json,
                   initial_objective, final_objective, evaluation_count, created_at_utc
            FROM calibration_runs
            ORDER BY created_at_utc DESC
            LIMIT $take;
            """;
        command.Parameters.AddWithValue("$take", take);
        using var reader = command.ExecuteReader();
        var results = new List<StoredCalibrationRun>();
        while (reader.Read())
        {
            results.Add(new StoredCalibrationRun
            {
                Id = Guid.Parse(reader.GetString(0)),
                MeasurementSourcePath = reader.GetString(1),
                BaselineConfigurationVersionId = reader.IsDBNull(2) ? null : Guid.Parse(reader.GetString(2)),
                FittedConfigurationVersionId = reader.IsDBNull(3) ? null : Guid.Parse(reader.GetString(3)),
                Algorithm = reader.GetString(4),
                ParameterIdsJson = reader.GetString(5),
                InitialValuesJson = reader.GetString(6),
                FittedValuesJson = reader.GetString(7),
                InitialObjective = reader.GetDouble(8),
                FinalObjective = reader.GetDouble(9),
                EvaluationCount = reader.GetInt32(10),
                CreatedAtUtc = DateTimeOffset.Parse(reader.GetString(11), CultureInfo.InvariantCulture)
            });
        }

        return results;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        SqliteConnection.ClearAllPools();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }
}
