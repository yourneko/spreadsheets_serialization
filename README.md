**SheetsIO** is a simple tool for exposing in-game data to Google Sheets in human-readable format, and providing Read & Edit access to it.
Other possible applications include:
* Instant delivery of configuration updates to client app;
* Aggregating logs from test devices;
* Back-ups & version control of user data.

**SheetsIO** uses Google Sheets API, so authenticating to the app requires **Google Credentials**.
Get the credentials with Google Sheets API web interface: https://developers.google.com/sheets/api

**SheetsIO** uses *Attributes* for storing metadata. Look for Attribute usage examples in **Assets/Scripts/Example/Data/**.

To read or write the data, create a new instance of *SheetsIO* using credentials. See **Assets/Scripts/Example/ExampleComponent.cs**

The spreadsheet used in example script: https://docs.google.com/spreadsheets/d/1SpCGl_1DkSfGl9hX8D4Ale7ektMRwOlr8Q1y9QJISHM
