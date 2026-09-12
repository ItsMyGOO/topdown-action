extends SceneTree
## 程序化像素资产生成器（Godot 无头脚本，可再生）：
## godot --headless --path . -s res://Tools/PixelArtGenerator.gd
## 生成 player_sheet.png / enemy_sheet.png / tileset.png 到 res://Game/Art/Pixel/

const OUT_DIR := "res://Game/Art/Pixel"

# 玩家调色板
const SKIN := Color8(232, 184, 138)
const TUNIC := Color8(74, 127, 212)
const TUNIC_DARK := Color8(58, 100, 170)
const HAIR := Color8(72, 52, 40)
const PANTS := Color8(52, 60, 84)
const OUTLINE := Color8(26, 26, 46)

# 怪物调色板
const SLIME := Color8(210, 70, 70)
const SLIME_DARK := Color8(160, 44, 44)
const EYE := Color8(255, 255, 255)
const PUPIL := Color8(20, 20, 30)

# 地面调色板
const GRASS_A := Color8(74, 110, 60)
const GRASS_B := Color8(88, 126, 68)
const DIRT := Color8(110, 84, 60)
const SPECK := Color8(60, 90, 50)


func _initialize() -> void:
	DirAccess.make_dir_recursive_absolute(OUT_DIR)
	_generate_player()
	_generate_enemy()
	_generate_tileset()
	quit(0)


func _rect(img: Image, x0: int, y0: int, x1: int, y1: int, color: Color) -> void:
	for y in range(y0, y1 + 1):
		for x in range(x0, x1 + 1):
			if x >= 0 and x < img.get_width() and y >= 0 and y < img.get_height():
				img.set_pixel(x, y, color)


func _generate_player() -> void:
	var f := 24
	var img := _new_image(f * 4, f * 4)
	for row in 4:  # 0 下 1 上 2 左 3 右
		for col in 4:
			var ox := col * f
			var oy := row * f
			var swing: int = [0, 1, 0, -1][col]
			# 头部
			_rect(img, ox + 8, oy + 4, ox + 15, oy + 11, SKIN)
			_rect(img, ox + 7, oy + 3, ox + 16, oy + 5, HAIR)
			match row:
				0:  # 下：双眼
					_px2(img, ox + 10, oy + 8, OUTLINE)
					_px2(img, ox + 13, oy + 8, OUTLINE)
				1:  # 上：后脑勺全发
					_rect(img, ox + 8, oy + 6, ox + 15, oy + 11, HAIR)
				2:  # 左：单眼靠左
					_px2(img, ox + 9, oy + 8, OUTLINE)
				3:  # 右：单眼靠右
					_px2(img, ox + 14, oy + 8, OUTLINE)
			# 躯干
			_rect(img, ox + 8, oy + 12, ox + 15, oy + 18, TUNIC)
			_rect(img, ox + 8, oy + 17, ox + 15, oy + 18, TUNIC_DARK)
			# 手臂（随步幅前后）
			_rect(img, ox + 6, oy + 12 + swing, ox + 7, oy + 17 + swing, SKIN)
			_rect(img, ox + 16, oy + 12 - swing, ox + 17, oy + 17 - swing, SKIN)
			# 腿（交替抬腿）
			_rect(img, ox + 9, oy + 19, ox + 11, oy + 23 - maxi(swing, 0), PANTS)
			_rect(img, ox + 12, oy + 19 + mini(swing, 0), ox + 14, oy + 23, PANTS)
			_rect(img, ox + 9, oy + 23, ox + 14, oy + 23, OUTLINE)
	img.save_png(OUT_DIR + "/player_sheet.png")


func _generate_enemy() -> void:
	var img := _new_image(16 * 4, 16)
	var squash: Array = [10, 12, 10, 8]
	for col in 4:
		var ox := col * 16
		var h: int = squash[col]
		var w := 12
		var top := 15 - h
		for y in range(top, 16):
			var t := float(y - top) / float(h)
			var half := int(w / 2.0 * (0.55 + 0.45 * sin(t * PI)))
			_rect(img, ox + 8 - half, y, ox + 8 + half, y, SLIME)
		_rect(img, ox + 8 - int(w / 2.0), 15, ox + 8 + int(w / 2.0), 15, SLIME_DARK)
		# 眼睛
		var eye_y := top + 3
		_rect(img, ox + 5, eye_y, ox + 6, eye_y + 1, EYE)
		_rect(img, ox + 9, eye_y, ox + 10, eye_y + 1, EYE)
		_px2(img, ox + 6, eye_y, PUPIL)
		_px2(img, ox + 9, eye_y, PUPIL)
	img.save_png(OUT_DIR + "/enemy_sheet.png")


func _generate_tileset() -> void:
	var img := _new_image(32 * 3, 32)
	var base: Array = [GRASS_A, GRASS_B, DIRT]
	for tile in 3:
		var ox := tile * 32
		_rect(img, ox, 0, ox + 31, 31, base[tile])
		# 确定性噪点
		var state := 1234 + tile * 77
		for i in 40:
			state = (state * 1103515245 + 12345) & 0x7FFFFFFF
			var x := ox + state % 32
			state = (state * 1103515245 + 12345) & 0x7FFFFFFF
			var y := state % 32
			_px2(img, x, y, SPECK)
	img.save_png(OUT_DIR + "/tileset.png")


func _new_image(w: int, h: int) -> Image:
	return Image.create(w, h, false, Image.FORMAT_RGBA8)


func _px2(img: Image, x: int, y: int, color: Color) -> void:
	if x >= 0 and x < img.get_width() and y >= 0 and y < img.get_height():
		img.set_pixel(x, y, color)
