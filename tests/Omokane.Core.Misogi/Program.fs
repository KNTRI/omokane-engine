module Program

open Expecto

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv ObservationGenerationTests.全テスト
