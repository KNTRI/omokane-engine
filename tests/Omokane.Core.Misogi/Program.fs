module Program

open Expecto

[<Tests>]
let 全テスト =
    testList
        "思兼神Core 禊Test"
        [
            ObservationGenerationTests.全テスト
            SensingTests.全テスト
            CausalOperationExecutionTests.全テスト
            CausalOperationBatchExecutionTests.全テスト
            CausalLedgerTests.全テスト
            CausalReplayTests.全テスト
        ]

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv 全テスト
