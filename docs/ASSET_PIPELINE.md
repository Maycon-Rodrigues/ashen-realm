# Pipeline de assets

Concept art → modelo procedural ou geração opcional via Meshy → normalização no Blender → importação e revisão na Unity.

## Blender headless

Use Blender 4.5 LTS. No Omarchy, o binário oficial portátil 4.5.3 executou as exportações do sprint. O diagnóstico também encontrou Blender no repositório Arch Extra via `pacman -Si blender`. [Distribuição oficial do Blender 4.5](https://download.blender.org/release/Blender4.5/)

O comando no README gera rocha facetada, árvore seca, pilar, ruína e altar. Restrinja a saída com `--asset Rock`, varie com `--seed` ou escolha `--format glb`. O modo `import` aceita FBX, GLB ou glTF de cenário estático.

```bash
blender --background --python-exit-code 1 \
  --python Tools/Blender/scripts/environment.py -- import /caminho/modelo.glb \
  --scale 0.5 --rotation-z 25 --output-dir Assets/_Game/Art/Generated --verify
```

O script seleciona malhas, preserva transformações da hierarquia na geometria, ajusta a base ao chão e adiciona um material neutro a malhas sem material. Ele valida coordenadas, faces degeneradas e orçamento de triângulos. `--verify` reimporta o resultado e confere a contagem de triângulos. Texturas e aparência final ainda precisam de revisão visual.

## Unity

Prefira FBX para importação nativa neste projeto. Use GLB para intercâmbio antes da conversão para FBX. Os modelos em `Art/Generated` são intermediários locais. Para integrar um modelo definitivo, promova o FBX e seu `.meta` para uma pasta versionada e prepare seu material URP e collider no prefab.

O CombatSandbox usa `Rock.fbx`, `DeadTree.fbx` e `Altar.fbx` de `Assets/_Game/Art/Arena`. Eles foram gerados com a seed padrão 71. Para atualizar um modelo, gere o FBX com `--verify`, substitua somente o FBX correspondente em `Art/Arena` e preserve seu `.meta`. A cena reaproveita as colisões dos pilares que esses modelos decoram.

Os sons de ataque e impacto em `Assets/_Game/Audio` podem ser recriados com `python Tools/Audio/generate-combat-cues.py`.

Para gerar de novo o kit visual do sandbox, use Blender 4.5 LTS. O script valida a geometria e reimporta cada FBX para conferir os triângulos.

```bash
blender --background --python-exit-code 1 \
  --python Tools/Blender/scripts/vertical_slice.py -- \
  --output-dir Assets/_Game/Art/VerticalSlice
```

Mantenha os `.meta` ao regenerar. Os nomes dos objetos indicam a superfície e a parte do modelo. O Editor usa esses nomes para aplicar os materiais, identificar a frente dos personagens e montar os pivôs das pernas.

Depois de gerar `Rock.fbx` pelo README e rodar o bootstrap, confirme que a Unity encontrou uma malha no modelo.

```bash
"$UNITY_EDITOR" -batchmode -nographics -quit -projectPath "$PWD" \
  -executeMethod AshenRealm.Editor.CombatSandboxBuilder.ValidateBlenderAsset \
  -logFile "$PWD/Logs/unity-fbx.log"
```

## Fallen Knight

A folha de conceito e o `.blend` editável ficam em `ArtSource/Characters/`. A arena consome o FBX versionado, com o esqueleto, os pesos e a pintura por vértice. Use o comando abaixo com Blender 4.5 LTS.

```bash
blender --background --python-exit-code 1 \
  --python Tools/Blender/scripts/fallen_knight.py -- \
  --blend ArtSource/Characters/FallenKnight.blend \
  --fbx Assets/_Game/Art/Characters/FallenKnightSkinned.fbx
```

O script verifica a malha, a cobertura dos pesos e as cores antes e depois da exportação. Preserve o `.meta` do FBX ao regenerar. Execute o bootstrap da arena para integrar um projeto que ainda usa o modelo anterior. O Editor verifica o skin e lê os slots de material do FBX para aplicar a paleta URP.

O FBX carrega a pose de repouso e a deformação. A Unity controla a caminhada, o manto e o golpe pelos ossos; o encaixe `SwordGrip` move a espada que o jogo já usa. Preserve os nomes dos ossos e dos materiais ao editar o modelo.

## Meshy opcional

Considere Meshy para personagens e criaturas que justifiquem uma etapa de geração. O serviço documenta exportação em FBX e GLB, e os formatos e limites variam conforme o fluxo e o plano. Para produção comercial, registre a origem e as condições de licença de cada modelo. A página de planos informa CC BY 4.0 para modelos gerados no plano gratuito, com atribuição. [Formatos de exportação do Meshy](https://docs.meshy.ai/en/webapp/guides/platform/export-formats), [Planos e licenças](https://www.meshy.ai/pricing)

Para personagens, avalie topologia, orçamento, UVs, texturas, pesos de skinning, rig, clips e desempenho como etapas próprias. O pipeline de cenário rejeita armatures, animações e shape keys.
