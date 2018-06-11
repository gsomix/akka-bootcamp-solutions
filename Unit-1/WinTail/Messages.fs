namespace WinTail

module Messages =
    // The user didn't provide any input, or the input was not valid.
    type ErrorType =
    | Null
    | Validation

    // Discriminated union to determine whether or not the user input was valid.
    type InputResult =
    | InputSuccess of string
    | InputError of reason: string * errorType: ErrorType