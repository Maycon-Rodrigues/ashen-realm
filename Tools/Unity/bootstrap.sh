#!/usr/bin/env bash
set -euo pipefail

root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
editor="${UNITY_EDITOR:-Unity}"
editor="$(command -v -- "$editor")" || {
  echo 'Informe UNITY_EDITOR com o caminho ao executável Unity 6.3 LTS.' >&2
  exit 1
}
if [[ "$editor" != /* ]]; then
  editor="$(realpath -- "$editor")"
fi

mkdir -p "$root/Logs"
if [[ ! -f "$root/ProjectSettings/ProjectVersion.txt" ]]; then
  if [[ -e "$root/ProjectSettings" ]]; then
    echo 'ProjectSettings já existe sem ProjectVersion.txt. Inspecione-a antes de continuar.' >&2
    exit 1
  fi
  work="$(mktemp -d)"
  trap 'rm -rf -- "$work"' EXIT
  "$editor" -batchmode -nographics -quit -createProject "$work/Stock" -logFile "$root/Logs/unity-create.log"
  version="$(<"$work/Stock/ProjectSettings/ProjectVersion.txt")"
  if [[ "$version" != 'm_EditorVersion: 6000.3.'* ]]; then
    echo 'Use um Editor da linha 6000.3 LTS.' >&2
    exit 1
  fi
  cp -a -- "$work/Stock/ProjectSettings" "$root/"
fi

"$editor" -batchmode -nographics -quit -projectPath "$root" \
  -executeMethod AshenRealm.Editor.CombatSandboxBuilder.Generate -logFile "$root/Logs/unity-sandbox.log"
if [[ ! -s "$root/Assets/_Game/Generated/CombatSandbox.unity" ]] ||
   ! grep -qF 'Ashen Realm: CombatSandbox ready at ' "$root/Logs/unity-sandbox.log"; then
  echo 'O Editor não confirmou a geração da cena. Consulte Logs/unity-sandbox.log.' >&2
  exit 1
fi
echo 'Cena pronta. Consulte Logs/unity-sandbox.log e abra o projeto em um novo processo Unity.'
