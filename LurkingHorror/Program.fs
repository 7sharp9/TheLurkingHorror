// Learn more about F# at http://fsharp.net
open System
open System.Diagnostics 
type internal ThrottlingAgentMessage = 
  | Message of string * int
  | Lock
  | Unlock
    
type badAgent(limit) = 
  
  let agent = MailboxProcessor.Start(fun agent -> 
    let sw = Stopwatch()
    let rec waiting () = 
      agent.Scan(function
        | Unlock -> Some(working ())
        | _ -> None)

    and working() = async { 
      let! msg = agent.Receive()
      match msg with 
      | Lock ->   return! waiting()
      | Unlock -> return! working()
      | Message (msg, iter) ->
          if iter = 0 then sw.Start()
          if iter % 10000 = 0 
            then sw.Stop()
                 printfn "%s : %i in: %fms" msg iter sw.Elapsed.TotalMilliseconds
                 sw.Restart()
                 sw.Start()
          return! working() }
    working())      

  member x.Msg(msg) = agent.Post(Message msg)
  member x.Lock() = agent.Post(Lock)
  member x.Unlock() = agent.Post(Unlock)

let ta = badAgent()

let message = "A message"
Console.WriteLine("Press and key to start")
Console.ReadLine() |> ignore
let twoMill() = 
    for i in 0 .. 200000 do
        ta.Msg(message, i)

ta.Lock()
twoMill()
ta.Unlock()

Console.ReadLine() |> ignore