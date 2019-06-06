module PanelTrigger

open Microsoft.Extensions.Logging
open Microsoft.Azure.WebJobs

open LiveData
open Microsoft.WindowsAzure.Storage.Table
open FSharp.Azure.Storage.Table
open AzureTableUtils
open System
open Microsoft.Azure.EventHubs
open System.Text

type PanelInfo =
     { [<PartitionKey>] DateStamp: string
       [<RowKey>] Id: string
       Panel: string
       MessageId: string
       Current: float // Iin#
       Volts: float // Vin#
       Watts: float // Pin#
       MessageTimestamp: DateTime // SysTime
       CorrelationId: string }

[<FunctionName("PanelTrigger")>]
let trigger
    ([<EventHubTrigger("live-data", ConsumerGroup = "Panel", Connection = "IoTHubConnectionString")>] eventData: EventData)
    ([<Table("PanelData", Connection = "DataStorageConnectionString")>] panelDataTable: CloudTable)
    (logger: ILogger) =
    async {
        let message = Encoding.UTF8.GetString eventData.Body.Array
        let correlationId = eventData.Properties.["correlationId"].ToString()
        let messageId = eventData.Properties.["messageId"].ToString()

        let parsedData = LiveDataDevice.Parse message
        let findPoint' = findPoint parsedData.Points
        let deviceId = parsedData.DeviceId.ToString()
        let timestamp = epoch.AddSeconds(findPoint' "SysTime")

        let storePanel p =
               let panel =
                   { DateStamp = timestamp.ToString("yyyy-MM-dd")
                     Panel = p
                     Id = Guid.NewGuid().ToString()
                     MessageId = messageId
                     Current = findPoint' (sprintf "Iin%s" p)
                     Volts = findPoint' (sprintf "Vin%s" p)
                     Watts = findPoint' (sprintf "Pin%s" p)
                     MessageTimestamp = timestamp
                     CorrelationId = correlationId }
               panel |> Insert |> inTableToClientAsync panelDataTable

        let! _ = storePanel "1"
        let! _ = storePanel "2"

        logger.LogInformation(sprintf "%s: Stored panel %s for device %s" correlationId messageId deviceId)
    } |> Async.StartAsTask