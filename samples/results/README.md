# AWG result samples

Generate a DOC-029 export package from the console:

```bash
dotnet run --project src/ThermoCore.Console -- run samples/awg-v3-mvp.json --duration 30 --dt 1 --export samples/results/awg-v3-mvp-smoke
```

The package includes `manifest.json`, configuration snapshot, CSV/JSON series, diagnostics, balances, and `balance-verification.json` (AWG-017/018/019).
