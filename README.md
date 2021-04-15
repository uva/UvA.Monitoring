# UvA.Monitoring

This is a tool for monitoring the event stream from the [Office 365 Management API](https://docs.microsoft.com/en-us/office/office-365-management-api/office-365-management-activity-api-reference) and reporting certain events to HTTP endpoints. Currently it reports three types of events:
- Stream videos being set to public
- Members being added to groups
- Teams being set to public

The tool can run as an Azure Function (`UvA.Monitoring.Functions`) that uses a webhook to receive events from the management API or a worker (e.g. Azure Web Job, `UvA.Monitoring.Worker`) that polls the API.
