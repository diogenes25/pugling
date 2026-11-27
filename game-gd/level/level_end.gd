class_name LevelEnd
extends Object
## Why a level ended; the server alone decides whether it counts as won.

enum Reason {
	## The goal of correct words is reached.
	GOAL_REACHED,
	## Too many skeletons reached the castle.
	CASTLE_FALLEN,
	## Today's play time is used up.
	TIME_UP,
	## The player stopped from the pause menu.
	GAVE_UP,
}

## The reasons as the log's end line spells them; the run reports are read by these words.
const LOG_NAMES: Dictionary[Reason, String] = {
	Reason.GOAL_REACHED: "GoalReached",
	Reason.CASTLE_FALLEN: "CastleFallen",
	Reason.TIME_UP: "TimeUp",
	Reason.GAVE_UP: "GaveUp",
}
