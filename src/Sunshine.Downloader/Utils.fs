module Utils
open Microsoft.Azure.Devices.Client
open System.Text
open System

let inline toS o = o.ToString()

let sendIoTMessage<'T> (client : DeviceClient) route (obj : 'T) =
    let json = obj |> toS
    let msg = new Message(Encoding.ASCII.GetBytes json)
    msg.Properties.Add("messageType", route)
    client.SendEventAsync(msg) |> Async.AwaitTask