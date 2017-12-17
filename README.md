# Elmah.Io.Export
Client for exporting messages from elmah.io

Examples:

```bash
dotnet Elmah.Io.Export.dll -ApiKey c7e049966ddf450f8ce6aeded7b581d0 -LogId 9f01ca78-174a-4a96-9f84-a336917a9deb
```

Export all messages from a log to a JSON file.

```bash
dotnet Elmah.Io.Export.dll -ApiKey c7e049966ddf450f8ce6aeded7b581d0 -LogId 9f01ca78-174a-4a96-9f84-a336917a9deb -Filename export.json -DateFrom 2017-01-01 -DateTo 2017-12-31 -Query statusCode:500 -IncludeHeaders
```

Export all messages (including headers) with status code 500 from 2017.