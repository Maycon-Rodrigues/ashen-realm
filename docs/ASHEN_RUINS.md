# As Ruínas das Cinzas

Primeiro bioma modular de Ashen Realm. A sala de combate fica sobre um desfiladeiro de basalto. As pranchas fornecidas para o bioma orientam a composição e o acabamento. [Referência da arena](../ArtSource/Environment/AshenRuins/arena-reference.webp) · [Referência do kit](../ArtSource/Environment/AshenRuins/modules-reference.webp)

Uma textura ilustrada de basalto rachado liga as lajes e cobre os blocos de alvenaria. A projeção em coordenadas do mundo continua entre módulos adjacentes. Muros baixos em ruínas cercam as bordas próximas da câmera. O memorial, os pilares e o arco criam os pontos de maior altura no lado oposto. A folhagem rubra e a luz âmbar dos braseiros se concentram nas bordas para deixar o centro disponível para combate.

## Kit inicial

| Família | Módulos |
| --- | --- |
| Piso | Laje, laje queimada e selo ritual |
| Alvenaria | Muro inteiro, muro partido, pilar e arco selado |
| Cenografia | Estandarte rasgado, braseiro, arbusto rubro, freixo queimado, escarpa de basalto, entulho e memorial |

Os pisos e os muros encaixam em uma malha de 2 m. O ponto de origem de cada peça fica no ponto de encaixe. A escarpa desce a partir de sua origem e o selo fica ligeiramente acima do piso. O gerador do Blender aplica pigmento e valida a geometria na reimportação dos 13 FBX. A ferramenta do Unity converte os modelos em prefabs com materiais URP e adiciona a luz local ao braseiro.

O desfiladeiro distante é uma ilustração presa ao enquadramento da câmera ortográfica. O memorial, as copas, os arbustos e as chamas usam recortes ilustrados posicionados no mundo 3D. As chamas oscilam no shader e o interior do arco usa `RuinsEmbers`. Os 5 PNGs ficam em `Assets/_Game/Art/AshenRuins/Illustrations`. [Prompts e método de geração](../ArtSource/Environment/AshenRuins/illustration-prompts.md)

## Montagem com seed

A cena `AshenRuins` usa 77 módulos de piso em uma área de 22 × 14 m. Uma seed inteira decide a posição do arco e do selo, quais trechos de muro ficam em ruínas e a variação das lajes e da folhagem. `AshenRuinsRoom.Rebuild(seed)` aplica a mesma distribuição em runtime. A área de colisão e a NavMesh são comuns às variações, porque o kit troca a cenografia ao redor da faixa jogável. O arco selado é um elemento cenográfico.

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

No Unity 6.3 LTS, os testes passaram em modo headless e com a GPU, com 2 aprovações e 0 falhas em cada execução. O teste do bioma compara a posição, a rotação e a escala de cada peça ao recriar a mesma seed, verifica que outra seed produz uma distribuição diferente e confirma que a NavMesh conecta os pontos iniciais do jogador e do inimigo e respeita o fim da passarela.

## Expansão do bioma

- Juntas, escadas e pontes podem usar a mesma unidade de 2 m. Módulos que mudam a área caminhável precisarão contribuir para a colisão e a navegação da sala.
- Estátuas, correntes, urnas, ossadas, árvores e raízes ampliam o kit de borda. Cristais, fissuras e barricadas entram quando houver regras de geração para perigos e cobertura.
- Portais de transição, baús, altares e santuários pertencem à futura integração com o grafo de salas e o estado da run.
