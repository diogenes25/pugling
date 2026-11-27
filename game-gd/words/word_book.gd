class_name WordBook
extends RefCounted
## A vocabulary word lying on the field as a book.

## The word.
var data: TowerToken

## Its place on the ground.
var pos: Vector2

## The book's model.
var node: Node3D

## Laid down by the hero: it stays until he takes it again.
var placed: bool = false

## Seconds on the field (only counts for dropped books).
var age: float = 0.0
