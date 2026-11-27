class_name TowerToken
extends RefCounted
## A vocabulary word of the run that drops as a book.

## The server's id of the token.
var token_id: String

## The word on the book.
var word: String


func _init(id: String, written: String) -> void:
	token_id = id
	word = written
