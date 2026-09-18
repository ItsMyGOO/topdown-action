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
	_generate_orb()
	_generate_markers()
	_generate_projectile()
	quit(0)


func _generate_markers() -> void:
	# 传送门（蓝）：24x32 拱形
	var portal := _new_image(24, 32)
	for y in range(4, 32):
		for x in range(24):
			var cx := absf(x - 11.5)
			var half := 3.0 + (y - 4) * 0.35
			if cx <= half:
				portal.set_pixel(x, y, Color8(70, 120, 235))
			if cx > half - 1.4:
				portal.set_pixel(x, y, Color8(30, 50, 120))
	portal.set_pixel(11, 14, Color8(200, 225, 255))
	portal.set_pixel(12, 18, Color8(200, 225, 255))
	portal.save_png(OUT_DIR + "/portal.png")

	# 地下城门（红）：同形红色
	var dungeon := Image.create(24, 32, false, Image.FORMAT_RGBA8)
	for y in range(4, 32):
		for x in range(24):
			var cx := absf(x - 11.5)
			var half := 3.0 + (y - 4) * 0.35
			if cx <= half:
				dungeon.set_pixel(x, y, Color8(200, 60, 60))
			if cx > half - 1.4:
				dungeon.set_pixel(x, y, Color8(90, 24, 24))
	dungeon.save_png(OUT_DIR + "/dungeon_portal.png")

	# 职业石像（灰石碑）：20x28
	var statue := Image.create(20, 28, false, Image.FORMAT_RGBA8)
	for y in range(2, 28):
		for x in range(4, 16):
			statue.set_pixel(x, y, Color8(140, 140, 150))
	_rect(statue, 6, 0, 13, 2, Color8(140, 140, 150))
	_rect(statue, 2, 26, 17, 27, Color8(90, 90, 100))
	_rect(statue, 8, 6, 11, 14, Color8(90, 90, 100))
	statue.save_png(OUT_DIR + "/class_statue.png")

	# 等级石像（金碑）：20x28
	var tier := Image.create(20, 28, false, Image.FORMAT_RGBA8)
	for y in range(2, 28):
		for x in range(4, 16):
			tier.set_pixel(x, y, Color8(196, 164, 90))
	_rect(tier, 6, 0, 13, 2, Color8(196, 164, 90))
	_rect(tier, 2, 26, 17, 27, Color8(130, 108, 60))
	_rect(tier, 8, 6, 11, 14, Color8(130, 108, 60))
	tier.save_png(OUT_DIR + "/tier_statue.png")

	# 仓库箱（棕木箱）：24x20
	var chest := Image.create(24, 20, false, Image.FORMAT_RGBA8)
	_rect(chest, 1, 4, 22, 19, Color8(130, 90, 50))
	_rect(chest, 1, 4, 22, 8, Color8(150, 106, 60))
	_rect(chest, 1, 11, 22, 12, Color8(90, 62, 34))
	_rect(chest, 10, 10, 13, 14, Color8(200, 180, 90))
	chest.save_png(OUT_DIR + "/stash_box.png")


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


func _generate_orb() -> void:
	var img := _new_image(12, 12)
	var center := Vector2(5.5, 5.5)
	for y in 12:
		for x in 12:
			var d := Vector2(x, y).distance_to(center)
			if d <= 5.5:
				if d > 4.2:
					img.set_pixel(x, y, Color8(120, 20, 24))
				elif d > 3.0:
					img.set_pixel(x, y, Color8(206, 48, 48))
				else:
					img.set_pixel(x, y, Color8(255, 130, 120))
	img.save_png(OUT_DIR + "/orb.png")


func _generate_projectile() -> void:
	# 投射物：8x8 蓝白能量弹（带尾迹感）
	var img := _new_image(8, 8)
	var bolt := Color8(120, 200, 255)
	var core := Color8(230, 245, 255)
	var tail := Color8(60, 120, 220)
	_rect(img, 2, 1, 5, 6, bolt)
	_rect(img, 3, 2, 3, 4, core)
	_rect(img, 1, 3, 1, 2, tail)
	_rect(img, 6, 3, 1, 2, tail)
	img.save_png(OUT_DIR + "/projectile.png")


func _new_image(w: int, h: int) -> Image:
	return Image.create(w, h, false, Image.FORMAT_RGBA8)


func _px2(img: Image, x: int, y: int, color: Color) -> void:
	if x >= 0 and x < img.get_width() and y >= 0 and y < img.get_height():
		img.set_pixel(x, y, color)
