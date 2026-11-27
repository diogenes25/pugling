class_name ApiResult
extends RefCounted
## The outcome of one server call: either the parsed JSON object or a German message ready for the screen.

## The call succeeded and [member data] holds the answer.
var ok: bool = false

## The JSON object of a successful call (camelCase keys; numbers arrive as float, read ids with int()).
var data: Dictionary = {}

## The message for the screen when the call failed.
var error: String = ""

## The server took no more assignments for this run (ended, or the grace time after the limit is over).
var run_closed: bool = false


## A successful call with its JSON object.
static func success(json: Dictionary) -> ApiResult:
	var result := ApiResult.new()
	result.ok = true
	result.data = json
	return result


## A failed call with its message for the screen.
static func failure(message: String, closed: bool = false) -> ApiResult:
	var result := ApiResult.new()
	result.error = message
	result.run_closed = closed
	return result
