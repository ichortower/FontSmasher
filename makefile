MOD_NAME=FontSmasher
SAMPLE_PACK=FontSmasherSamplePack
MODE?=GOG
GAME_DIR=${HOME}/GOG Games/Stardew Valley/game
ifeq (${MODE}, steam)
	GAME_DIR=${HOME}/.steam/steam/steamapps/common/Stardew Valley
endif
MOD_DIR=${GAME_DIR}/Mods/${MOD_NAME}
SAMPLE_DIR=${GAME_DIR}/Mods/${SAMPLE_PACK}

install: smapi

smapi:
	MODE=${MODE} dotnet build /clp:NoSummary
	install -m 644 LICENSE "${MOD_DIR}"

colors: docs/color-table.md

docs/color-table.md: docs/color-data.txt
	./docs/generate-svgs.bash docs/svg <"$<"
	awk -f docs/generate-table.awk "$<" >"$@"

samplepack:
	mkdir -p "${SAMPLE_DIR}/assets"
	install -m 644 "${SAMPLE_PACK}"/assets/*.png "${SAMPLE_DIR}/assets/"
	install -m 644 "${SAMPLE_PACK}"/content.json "${SAMPLE_PACK}"/manifest.json "${SAMPLE_DIR}"
	install -m 644 LICENSE "${SAMPLE_DIR}"

clean:
	rm -rf bin obj

uninstall:
	rm -rf "${MOD_DIR}"
	rm -rf "${SAMPLE_DIR}"
