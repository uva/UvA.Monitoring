# UvA.Monitoring

This is a tool for monitoring the event stream from the [Office 365 Management API](https://docs.microsoft.com/en-us/office/office-365-management-api/office-365-management-activity-api-reference) and reporting certain events to HTTP endpoints. Currently it reports three types of events:
- Stream videos being set to public
- Members being added to groups
- Teams being set to public

The tool can run as an Azure Function (`UvA.Monitoring.Functions`) that uses a webhook to receive events from the management API or a worker (e.g. Azure Web Job, `UvA.Monitoring.Worker`) that polls the API.

When an event occurs, an HTTP request is made to the configured url. For UvA/HvA these are handled by logic apps:

- Prod-Monitor-Public-Not-Allowed: [UvA](https://portal.azure.com/#@Amsuni.onmicrosoft.com/resource/subscriptions/803eb21c-da0d-4979-a63d-952c3e8e5546/resourceGroups/o365-automation-p-rg/providers/Microsoft.Logic/workflows/Prod-Monitor-Public-Not-Allowed/logicApp), [HvA](https://portal.azure.com/#@icthva.onmicrosoft.com/resource/subscriptions/20614c3f-9ee3-4859-bb13-78e0f82586d0/resourceGroups/automation-rg/providers/Microsoft.Logic/workflows/Prod-Monitor-Public-Not-Allowed/logicApp)
- Prod-Member-Monitoring: [UvA](https://portal.azure.com/#@Amsuni.onmicrosoft.com/resource/subscriptions/803eb21c-da0d-4979-a63d-952c3e8e5546/resourceGroups/o365-automation-p-rg/providers/Microsoft.Logic/workflows/Prod-Member-Monitoring/logicApp), [HvA](https://portal.azure.com/#@icthva.onmicrosoft.com/resource/subscriptions/20614c3f-9ee3-4859-bb13-78e0f82586d0/resourceGroups/automation-rg/providers/Microsoft.Logic/workflows/Prod-Member-Monitoring/logicApp)