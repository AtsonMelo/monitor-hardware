# Configuração

O arquivo `config.json` permite ajustar limites de alerta, modo de execução e comportamento do app.

Exemplo:

```json
{
  "CpuTempMax": 80,
  "GpuTempMax": 80,
  "SsdTempMax": 60,
  "IntervaloMs": 2000,
  "EnableCsv": true,
  "EnableConsole": true,
  "Mode": "resumo",
  "CpuFanSensorName": "Fan #2",
  "TemperatureUnit": "C",
  "ShowTemperatureUnitInTrayIcon": false,
  "EnableAutoUpdateCheck": true
}
```

Campos:

- `CpuTempMax`: temperatura máxima esperada para CPU, em graus Celsius;
- `GpuTempMax`: temperatura máxima esperada para GPU, em graus Celsius;
- `SsdTempMax`: temperatura máxima esperada para SSD, em graus Celsius;
- `IntervaloMs`: intervalo entre leituras, em milissegundos;
- `EnableCsv`: ativa ou desativa a gravação automática de CSV;
- `EnableConsole`: ativa ou desativa a saída no console;
- `Mode`: modo padrão. Valores: `resumo`, `detalhado` ou `somente-log`;
- `CpuFanSensorName`: nome do sensor usado como fan da CPU;
- `TemperatureUnit`: unidade de temperatura. Use `C` ou `F`;
- `ShowTemperatureUnitInTrayIcon`: mostra a unidade no ícone da bandeja;
- `EnableAutoUpdateCheck`: verifica automaticamente se existe versão nova no GitHub.

Se `IntervaloMs` for menor ou igual a zero, o app não inicia o monitoramento.
