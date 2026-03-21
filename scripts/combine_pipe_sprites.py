#!/usr/bin/env python3
"""
Combines simplified pipe sprites into a horizontal sprite sheet.
Uses the simplified direction-based naming convention.
"""

from pathlib import Path
from PIL import Image

# Sprite order matching PipeSpriteResolver.SpriteOrder in C#
SPRITE_ORDER = [
    # Horizontal (direction E or W)
    "iron_pipe_direction_E_or_W_pipe_E_and_W",      # 0
    "iron_pipe_direction_E_or_W_pipe_E_no_pipe_W",  # 1
    "iron_pipe_direction_E_or_W_pipe_W_no_pipe_E",  # 2
    "iron_direction_E_or_W_no_pipe_conection",      # 3

    # Vertical (direction N or S)
    "iron_pipe_direction_N_or_S_pipe_N_and_S",      # 4
    "iron_pipe_direction_N_or_S_pipe_N_no_pipe_S",  # 5
    "iron_pipe_direction_N_or_S_pipe_S_no_pipe_N",  # 6
    "iron_direction_N_or_S_no_pipe_conection",      # 7

    # Fallback
    "iron_else",                                     # 8

    # Corners
    "iron_pipe_direction_S_or_E_pipe_E_and_S",       # 9
    "iron_pipe_direction_N_or_E_pipe_E_and_N",       # 10
    "iron_pipe_direction_N_or_W_pipe_N_and_W",       # 11
    "iron_pipe_direction_W_or_S_pipe_W_and_S",       # 12

    # T-Junctions
    "iron_pipe_direction_N_or_E_or_W_pipe_E_and_S_and_W",   # 13
    "iron_pipe_direction_N_or_W_or_S_pipe_N_and_W_and_S",   # 14
    "iron_pipe_direction_E_or_W_or_S_pipe_E_and_W_and_S",   # 15
    "iron_pipe_direction_N_or_E_or_S_pipe_N_and_E_and_S",   # 16

    # 4-Way Junction
    "iron_pipe_direction_N_or_S_pipe_all_4_E_and_W_pointing_in",   # 17
]

SPRITE_SIZE = 16


def combine_sprites(input_dir: Path, output_path: Path):
    """Combine individual sprite PNGs into a horizontal sprite sheet."""

    sprites = []
    missing = []

    for sprite_name in SPRITE_ORDER:
        sprite_path = input_dir / f"{sprite_name}.png"
        if sprite_path.exists():
            img = Image.open(sprite_path)
            if img.size != (SPRITE_SIZE, SPRITE_SIZE):
                print(f"Warning: {sprite_name}.png is {img.size}, resizing to {SPRITE_SIZE}x{SPRITE_SIZE}")
                img = img.resize((SPRITE_SIZE, SPRITE_SIZE), Image.NEAREST)
            sprites.append(img)
        else:
            missing.append(sprite_name)
            placeholder = Image.new('RGBA', (SPRITE_SIZE, SPRITE_SIZE), (255, 0, 255, 255))
            sprites.append(placeholder)

    if missing:
        print(f"Warning: Missing sprites: {missing}")

    # Create the combined sheet
    sheet_width = len(SPRITE_ORDER) * SPRITE_SIZE
    sheet_height = SPRITE_SIZE
    sheet = Image.new('RGBA', (sheet_width, sheet_height), (0, 0, 0, 0))

    for i, sprite in enumerate(sprites):
        sheet.paste(sprite, (i * SPRITE_SIZE, 0))

    sheet.save(output_path, 'PNG')
    print(f"Created: {output_path} ({sheet_width}x{sheet_height}, {len(SPRITE_ORDER)} sprites)")


def main():
    script_dir = Path(__file__).parent
    assets_dir = script_dir.parent / "assets" / "pipes"

    # Use the simplified sprites folder
    input_dir = assets_dir / "simplified_use_for_all_iron"
    output_path = assets_dir / "iron_sheet.png"

    if input_dir.exists():
        print(f"Processing simplified iron sprites from {input_dir}...")
        combine_sprites(input_dir, output_path)
    else:
        print(f"Error: Directory not found: {input_dir}")


if __name__ == "__main__":
    main()
