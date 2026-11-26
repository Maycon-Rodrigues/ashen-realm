# As Ruínas das Cinzas

Primeiro bioma modular de Ashen Realm. A sala de combate fica sobre um desfiladeiro de basalto. As pranchas fornecidas para o bioma orientam a composição e o acabamento. [Referência da arena](../ArtSource/Environment/AshenRuins/arena-reference.webp) · [Referência do kit](../ArtSource/Environment/AshenRuins/modules-reference.webp)

As lajes e a alvenaria usam tons de ameixa e argila com variação de pigmento nos vértices. A passarela de combate fica aberta no centro. Paredes partidas e braseiros mais baixos na borda próxima da câmera deixam o personagem visível. A borda distante concentra os pilares, os estandartes e o arco com o selo de cinzas. As folhagens vermelhas se agrupam nas bordas para que o aviso de ataque se destaque no piso.

## Kit inicial

| Família | Módulos |
| --- | --- |
| Piso | Laje, laje queimada e selo ritual |
| Alvenaria | Muro inteiro, muro partido, pilar e arco selado |
| Cenografia | Estandarte rasgado, braseiro, arbusto rubro, freixo queimado e escarpa de basalto |

Os pisos e os muros encaixam em uma malha de 2 m. O ponto de origem de cada peça fica no ponto de encaixe. A escarpa desce a partir de sua origem e o selo fica ligeiramente acima do piso. O gerador do Blender aplica pigmento e valida a geometria na reimportação dos 12 FBX. A ferramenta do Unity converte os modelos em prefabs com materiais URP e adiciona a luz local ao braseiro. A névoa abaixo da passarela e o brilho dentro do arco usam o shader `RuinsEmbers`.

## Montagem com seed

A cena `AshenRuins` usa 120 módulos de piso em uma área de 24 × 20 m. Uma seed inteira decide a posição do arco e do selo, quais trechos de muro ficam em ruínas e a variação das lajes e da folhagem. `AshenRuinsRoom.Rebuild(seed)` aplica a mesma distribuição em runtime. A área de colisão e a NavMesh são comuns às variações, porque o kit troca a cenografia ao redor da faixa jogável. O arco selado é um elemento cenográfico.

Abra `Assets/_Game/Generated/AshenRuins.unity` para jogar a seed 191. Use o menu **Ashen Realm → Generate Ashen Ruins** para remontar a cena a partir do `CombatSandbox`. Defina `ASHEN_RUINS_SEED` para gerar outra composição por CLI.

```bash
blender --background --python-exit-code 1 \
  --python Tools/Blender/scripts/ashen_ruins.py -- \
  --output-dir Assets/_Game/Art/AshenRuins

mkdir -p Logs
ASHEN_RUINS_SEED=522 "$UNITY_EDITOR" -batchmode -nographics -quit \
  -projectPath "$PWD" -executeMethod AshenRealm.Editor.AshenRuinsBuilder.Generate \
  -logFile "$PWD/Logs/ashen-ruins.log"
```

A geração por CLI grava a composição na cena. Para recriar a versão do repositório, use a seed 191. Preserve os arquivos `.meta` ao atualizar os modelos.

No Unity 6.3 LTS, os testes passaram em modo headless e com a GPU, com 2 aprovações e 0 falhas em cada execução. O teste do bioma compara a posição e a rotação de cada peça ao recriar a mesma seed, verifica que outra seed produz uma distribuição diferente e confirma que a NavMesh continua conectando os pontos iniciais do jogador e do inimigo.

## Expansão do bioma

- Juntas, escadas e pontes podem usar a mesma unidade de 2 m. Módulos que mudam a área caminhável precisarão contribuir para a colisão e a navegação da sala.
- Estátuas, correntes, urnas, ossadas, árvores e raízes ampliam o kit de borda. Cristais, fissuras e barricadas entram quando houver regras de geração para perigos e cobertura.
- Portais de transição, baús, altares e santuários pertencem à futura integração com o grafo de salas e o estado da run.
