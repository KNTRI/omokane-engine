namespace Omokane.Core.Validation

type 検証結果 =
    | 正常
    | エラー of string list
