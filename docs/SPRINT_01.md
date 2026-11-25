# Sprint 01 — Combat Sandbox

Objetivo: usar uma arena reproduzível para mover o Fallen Knight, entrar em combate e derrotar um Corrupted Grunt com três ataques básicos.

## Critérios de aceite

| Critério | Evidência disponível |
| --- | --- |
| Projeto abre e compila | Validado em Unity 6000.3.29f1 com licença Personal no Omarchy 4.5.0. |
| Cena CombatSandbox disponível | Gerada pelo Editor com bake da NavMesh e carregada pelo teste. |
| WASD e câmera | Play Mode validou teclado, analógico virtual e acompanhamento da câmera. |
| Ataque básico | Play Mode validou clique esquerdo, botão oeste virtual e cooldown. |
| Detecção, perseguição e dano inimigo | Play Mode verificou repouso inicial, perseguição e perda de HP. |
| Inimigo recebe dano e morre | Play Mode confirmou 45 → 25 → 5 → 0 HP, dano único por ataque com dois colliders e um evento de morte. |
| Console sem erros críticos | O teste passou sem logs de erro inesperados no Play Mode. |
| Ambiente reproduzível | Bootstrap Unity e pipeline Blender executados. Cena, configurações e lock dos pacotes versionados. |
| Asset automático importado na Unity | O Editor importou `Rock.fbx` e confirmou a presença da malha. |

## Evidência executada

Blender portátil 4.5.3 LTS, x86_64, checksum SHA-256 correspondente ao publicado. Os FBX Rock (80), DeadTree (144), Pillar (308), Ruin (308) e Altar (132) mantiveram a contagem de triângulos após reimportação. Rock também passou ida e volta via GLB e o fluxo GLB → normalização com escala e rotação → FBX. Os logs locais estão em `Logs/blender-*.log`.

O teste de orçamento rejeitou a rocha de 80 triângulos com limite de 79 e saiu com código 1 antes de gravar o FBX. A sintaxe do bootstrap e do Python, os arquivos JSON e os GUIDs das metas foram verificados. O bootstrap retornou código 1 com uma mensagem de diagnóstico quando o executável indicado estava ausente.

## Validação no Unity

Em 25/11/2026, a geração e o bake foram confirmados em `Logs/unity-sandbox.log`. A importação foi confirmada em `Logs/unity-fbx.log`. O Play Mode passou tanto em headless (`Logs/playmode.xml`) quanto com a GPU Intel/Mesa (`Logs/playmode-graphics.xml`), com 1 teste aprovado e 0 falhas em cada execução. A primeira execução precisou de um ajuste no teste para receber input virtual sem foco. A experiência com um gamepad físico permanece sem verificação.

Com o Editor gráfico fechado, execute o teste sem `-quit`, pois o Test Runner gerencia o encerramento.

```bash
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" \
  -runTests -testPlatform PlayMode \
  -testResults "$PWD/Logs/playmode.xml" -logFile "$PWD/Logs/unity-tests.log"
```

O comando de importação FBX está no pipeline de assets.
