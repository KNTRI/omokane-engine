module Program

open Expecto

[<Tests>]
let 全テスト =
    testList
        "思兼神Core 禊Test"
        [
            ObservationGenerationTests.全テスト
            SensingTests.全テスト
            BeliefCandidateTests.全テスト
            AlertIntentTests.全テスト
            GroundVibrationAlertSliceTests.全テスト
            AlertActionCandidateTests.全テスト
            GroundVibrationCausalEmissionTests.全テスト
            CausalGroundVibrationCognitiveLoopTests.全テスト
            CausalOperationExecutionTests.全テスト
            CausalOperationBatchExecutionTests.全テスト
            CausalLedgerTests.全テスト
            CausalReplayTests.全テスト
            IntegratedCausalLedgerTests.全テスト
            IntegratedCausalReplayTests.全テスト
            IntegratedCausalHistoryCompatibilityTests.全テスト
            ActionCandidateSelectionTests.全テスト
            EntityReactionRequestTests.全テスト
            EntityReactionStateCausalTests.全テスト
            IntegratedEntityReactionHistoryTests.全テスト
            VoluntaryLocomotionControlTests.全テスト
        ]

[<EntryPoint>]
let main argv =
    runTestsWithCLIArgs [] argv 全テスト
