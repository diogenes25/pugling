class_name HeroSkin
extends Resource
## One look of the hero as data (skin_knight.tres ...). What a skin costs and whether the child owns it is the
## server's business; the game only knows how it looks and what it is called.

## The id the server knows the skin by.
@export var id: String = ""

## What the skin is called on screen.
@export var title: String = ""

## The colour the knight's model is washed in; white leaves it as it is.
@export var tint: Color = Color.WHITE

