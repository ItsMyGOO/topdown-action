extends SceneTree
## 从骑士动作表自动构建玩家 SpriteFrames 资源（可再生）：
## godot --headless --path . -s res://Tools/BuildPlayerSpriteFrames.gd
## 输出 res://Game/Art/Player/player_spriteframes.tres
## 编辑器中可进一步可视化微调（AnimatedSprite2D 的 SpriteFrames）。

const OUT_PATH := "res://Game/Art/Player/player_spriteframes.tres"
const SPRITE_DIR := "res://Game/Art/2D HD Character Knight/Spritesheets/With shadows"

# 动作 → [表文件, 实际帧数, fps, 是否循环]
const ACTIONS := {
	"idle": ["Idle.png", 15, 12.0, true],
	"run": ["Run.png", 15, 14.0, true],
	"melee": ["Melee.png", 10, 16.0, false],
	"cast": ["CastSpell.png", 10, 14.0, false],
	"hurt": ["TakeDamage.png", 6, 12.0, false],
	"roll": ["Rolling.png", 13, 14.0, false],
}

# 行序（E 起顺时针）→ 方向后缀
const DIRECTIONS := ["e", "se", "s", "sw", "w", "nw", "n", "ne"]

const COLUMNS := 15
const ROWS := 8
const FRAME_W := 128
const FRAME_H := 128


func _initialize() -> void:
	var frames := SpriteFrames.new()
	frames.remove_animation("default")

	for action in ACTIONS.keys():
		var sheet_name: String = ACTIONS[action][0]
		var frame_count: int = ACTIONS[action][1]
		var fps: float = ACTIONS[action][2]
		var loop: bool = ACTIONS[action][3]
		var texture: Texture2D = load(SPRITE_DIR + "/" + sheet_name)

		for dir in DIRECTIONS:
			var row: int = DIRECTIONS.find(dir)
			var anim_name: String = action + "_" + dir
			frames.add_animation(anim_name)
			frames.set_animation_speed(anim_name, fps)
			frames.set_animation_loop(anim_name, loop)

			for col in frame_count:
				var atlas := AtlasTexture.new()
				atlas.atlas = texture
				atlas.region = Rect2(col * FRAME_W, row * FRAME_H, FRAME_W, FRAME_H)
				frames.add_frame(anim_name, atlas)

	var err := ResourceSaver.save(frames, OUT_PATH)
	print("saved ", OUT_PATH, " err=", err)
	quit(0)
