#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
GAME="${STS2_GAME_BIN:-/Users/iniad/Library/Application Support/Steam/steamapps/common/Slay the Spire 2/SlayTheSpire2.app/Contents/MacOS/Slay the Spire 2}"
PCK="${1:-$ROOT/dist/PRTSCursor.pck}"
OUT="$ROOT/.build/spine_cursor_render_full"
DEFAULT_DIR="$ROOT/assets/cursors/default"
DRAGGING_DIR="$ROOT/assets/cursors/dragging"
WORK_DIR="$OUT/default_work"
TEMPORAL_SIDE_WEIGHT=0.15
TEMPORAL_CENTER_WEIGHT=0.70
EXPECTED_DEFAULT_FRAMES="${PRTS_CURSOR_EXPECTED_FRAMES:-1362}"

rm -rf "$OUT"
mkdir -p "$OUT" "$WORK_DIR" "$DEFAULT_DIR"

"$GAME" --path "$ROOT/tools" -s res://render_spine_cursor_frames.gd -- "$PCK" "$OUT"

raw_frame_count="$(find "$OUT/default_raw" -name 'default_*.png' | wc -l | tr -d ' ')"
if [[ "$raw_frame_count" != "$EXPECTED_DEFAULT_FRAMES" ]]; then
	echo "Expected $EXPECTED_DEFAULT_FRAMES rendered default frames, got $raw_frame_count." >&2
	exit 5
fi

rm -f "$DEFAULT_DIR"/default_*.png
rm -rf "$DRAGGING_DIR"

# Use one fixed source-space crop for all frames. Per-frame trimming makes the
# image origin move and causes cursor hotspot jitter.
for src in "$OUT"/default_raw/default_*.png; do
	base="$(basename "$src")"
	magick "$src" -crop 224x224+16+8 +repage -resize 128x128 \
		-background none -gravity northwest -extent 128x128 "$WORK_DIR/$base"
done

# Smooth the fixed-crop animation lightly to damp tiny shape flickers without
# moving frames back toward any locked tip position.
frames=()
while IFS= read -r src; do
	frames+=("$src")
done < <(find "$WORK_DIR" -name 'default_*.png' -print | sort -V)

frame_count="${#frames[@]}"
if (( frame_count == 0 )); then
	echo "No default cursor frames were rendered." >&2
	exit 4
fi

for (( i = 0; i < frame_count; i++ )); do
	prev="${frames[$(((i + frame_count - 1) % frame_count))]}"
	cur="${frames[$i]}"
	next="${frames[$(((i + 1) % frame_count))]}"
	base="$(basename "$cur")"
	magick \
		\( "$prev" -evaluate multiply "$TEMPORAL_SIDE_WEIGHT" \) \
		\( "$cur" -evaluate multiply "$TEMPORAL_CENTER_WEIGHT" \) \
		\( "$next" -evaluate multiply "$TEMPORAL_SIDE_WEIGHT" \) \
		-evaluate-sequence sum "$DEFAULT_DIR/$base"
done

printf 'Temporal smoothing weights: previous=%s current=%s next=%s.\n' \
	"$TEMPORAL_SIDE_WEIGHT" "$TEMPORAL_CENTER_WEIGHT" "$TEMPORAL_SIDE_WEIGHT"
printf 'Tip stabilization disabled; fixed-crop frame origins are preserved.\n'

printf 'Baked %s default frames. Dragging/Click frames disabled.\n' \
	"$(find "$DEFAULT_DIR" -name 'default_*.png' | wc -l | tr -d ' ')"

final_frame_count="$(find "$DEFAULT_DIR" -name 'default_*.png' | wc -l | tr -d ' ')"
if [[ "$final_frame_count" != "$EXPECTED_DEFAULT_FRAMES" ]]; then
	echo "Expected $EXPECTED_DEFAULT_FRAMES baked default frames, got $final_frame_count." >&2
	exit 6
fi

empty_frame_count=0
for frame in "$DEFAULT_DIR"/default_*.png; do
	alpha_mean="$(magick "$frame" -alpha extract -format '%[fx:mean]' info:)"
	if awk -v alpha_mean="$alpha_mean" 'BEGIN { exit alpha_mean > 0 ? 1 : 0 }'; then
		empty_frame_count=$((empty_frame_count + 1))
	fi
done

if (( empty_frame_count > 0 )); then
	echo "Baked cursor output contains $empty_frame_count fully transparent frame(s)." >&2
	exit 7
fi
