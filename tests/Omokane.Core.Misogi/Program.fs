module Program

open Expecto

[<Tests>]
let 全テスト =
    testList
        "思兼神Core 禊Test"
        [
            CausalOperationExecutionTests.全テスト
            ObservationGenerationTests.全テスト
        ]

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv 全テスト
