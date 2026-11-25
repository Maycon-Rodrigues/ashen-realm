# Sprint 01 — Combat Sandbox

Objetivo: usar uma arena reproduzível para mover o Fallen Knight, entrar em combate e derrotar um Corrupted Grunt com três ataques básicos.

## Critérios de aceite

| Critério | Evidência disponível |
| --- | --- |
| Projeto abre e compila | Pendente. Unity Editor e licença ativa indisponíveis no ambiente inspecionado. |
| Cena CombatSandbox disponível | Gerador implementado. A execução e o bake aguardam o Editor. |
| WASD e câmera | Implementados. Há um teste de Play Mode preparado com entrada simulada. |
| Ataque básico | Implementado. O teste preparado simula clique esquerdo. |
| Detecção, perseguição e dano inimigo | Implementados. O teste preparado verifica repouso inicial, perseguição e perda de HP. |
| Inimigo recebe dano e morre | Implementado. O teste preparado exige 45 → 25 → 5 → 0 HP, desativação e um evento de morte. |
| Console sem erros críticos | Pendente de execução no Unity. |
| Ambiente reproduzível | Documentado. O pipeline Blender foi reproduzido. O bootstrap Unity aguarda execução. |
| Asset automático importado na Unity | Cinco FBX gerados e reimportados no Blender. Importação Unity pendente. |

## Evidência executada

Blender portátil 4.5.3 LTS, x86_64, checksum SHA-256 correspondente ao publicado. Os FBX Rock (80), DeadTree (144), Pillar (308), Ruin (308) e Altar (132) mantiveram a contagem de triângulos após reimportação. Rock também passou ida e volta via GLB e o fluxo GLB → normalização com escala e rotação → FBX. Os logs locais estão em `Logs/blender-*.log`.

O teste de orçamento rejeitou a rocha de 80 triângulos com limite de 79 e saiu com código 1 antes de gravar o FBX. A sintaxe do bootstrap e do Python, os arquivos JSON e os GUIDs das metas foram verificados. O bootstrap retornou código 1 com uma mensagem de diagnóstico quando o executável indicado estava ausente.

## Validação no Unity

Execute o bootstrap descrito no README. Com o Editor gráfico fechado, execute o teste sem `-quit`, pois o Test Runner gerencia o encerramento.

```bash
"$UNITY_EDITOR" -batchmode -nographics -projectPath "$PWD" \
  -runTests -testPlatform PlayMode \
  -testResults "$PWD/Logs/playmode.xml" -logFile "$PWD/Logs/unity-tests.log"
```

O teste usa teclado e mouse virtuais. Abra a cena no Editor para verificar visual, experiência de controle com um gamepad físico e mensagens do Console. O comando de importação FBX está no pipeline de assets.
