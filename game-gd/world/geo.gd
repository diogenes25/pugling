class_name Geo
extends Object
## Small geometry helpers on the ground plane. The game's logic runs in 2D ground coordinates (x, y), where y is the
## world's z axis; height only matters for drawing.


## A point on a path and the direction of travel there.
class PathPoint:
	extends RefCounted

	var pos: Vector2
	var dir: Vector2


## Lifts a ground point into the 3D world.
static func to3(p: Vector2, y: float = 0.0) -> Vector3:
	return Vector3(p.x, y, p.y)


## Drops a world point onto the ground plane.
static func flat(p: Vector3) -> Vector2:
	return Vector2(p.x, p.z)


## Distance from [param p] to the segment a-b.
static func dist_to_segment(p: Vector2, a: Vector2, b: Vector2) -> float:
	var ab := b - a
	var t := 0.0 if ab.length_squared() < 1e-6 else clampf((p - a).dot(ab) / ab.length_squared(), 0.0, 1.0)
	return p.distance_to(a + ab * t)


## Distance from [param p] to a polyline.
static func dist_to_polyline(p: Vector2, line: PackedVector2Array) -> float:
	var best := INF
	for i: int in range(1, line.size()):
		best = minf(best, dist_to_segment(p, line[i - 1], line[i]))
	return best


## Total length of a polyline.
static func length(line: PackedVector2Array) -> float:
	var sum := 0.0
	for i: int in range(1, line.size()):
		sum += line[i - 1].distance_to(line[i])
	return sum


## The point at [param distance] along a polyline, and the direction of travel there.
static func point_at(line: PackedVector2Array, distance: float) -> PathPoint:
	var point := PathPoint.new()
	for i: int in range(1, line.size()):
		var segment := line[i - 1].distance_to(line[i])
		point.dir = (line[i] - line[i - 1]).normalized()
		if distance <= segment:
			point.pos = line[i - 1] + point.dir * distance
			return point
		distance -= segment
	point.pos = line[line.size() - 1]
	return point
