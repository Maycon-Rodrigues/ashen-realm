# Direção de arte

Dark fantasy 3D com acabamento pictórico. Ruínas de basalto, pedra marcada pelo tempo, troncos mortos e superfícies foscas definem a arena. Volumes contínuos e chanfros arredondados diferenciam personagens e equipamentos. Reserve cor mais saturada para efeitos e elementos interativos.

| Uso | Cor sugerida |
| --- | --- |
| Fundo e sombra | `#121719` |
| Basalto | `#252D2D` |
| Pedra de cinza | `#5B5145` |
| Madeira seca | `#392B20` |
| Aço do Fallen Knight | `#477A85` |
| Ferrugem da corrupção | `#A64026` |

- Teste as silhuetas no ângulo da câmera do jogo. A direção dos atores deve ser reconhecível pelo contorno ou detalhes de cor.
- Use superfícies contínuas na pele, no elmo e no manto. Preserve planos largos e chanfros definidos na alvenaria. O aço pode ter reflexos pontuais e a pedra deve manter um acabamento mate.
- Modele em metros, com ponto de apoio no chão. Use colisores simples na Unity e valide escala, normais, UVs e custo de renderização.
- O Fallen Knight tem placas em aço azulado, pala assimétrica, broquel angular e tecido verde gasto nas costas. A fenda turquesa e a ombreira marcada ajudam a ler a direção do personagem. O Corrupted Grunt é mais baixo, com chifres, núcleo incandescente e um antebraço dominante que se ergue antes do golpe.
- A Lâmina de Cinza tem uma geometria própria para o objeto caído e para a arma na mão. A lâmina equipada fica turquesa. As chamas do portão são pequenas para preservar o contraste do setor laranja de ataque no chão.
- O piso de lajes destaca a faixa central e leva ao portão de pedra. Nas bordas do pátio, o altar com cinzas, velas e tecido, as pedras soltas e as lajes faltantes mostram sinais da vigília funerária. O portão permanece fechado nesta etapa.

O desenho das passagens e a organização visual do pátio têm como referência as cenas de *Death’s Door*. A diferenciação de silhueta e material por personagem parte da direção pictórica de *No Rest for the Wicked* e de peças históricas das Royal Armouries. A iluminação usa uma base fria e acentos quentes localizados para separar o cenário da ação. [Demonstração da Acid Nerve](https://www.youtube.com/watch?v=pcSmBGkbd-g), [Moon Studios](https://norestforthewicked.com/news/introducing-no-rest-for-the-wicked), [Armadura histórica](https://royalarmouries.org/collection/object/object-3178), [Arte e iluminação de Diablo IV](https://news.blizzard.com/en-us/article/23964183/peeling-back-the-varnish-the-graphics-of-diablo-iv)

## Produção

O Fallen Knight parte de uma [folha de conceito original](../ArtSource/Characters/FallenKnightConcept.png). A paleta usa contraste entre aço frio, tecido verde e guarnições em bronze. A pintura por vértice reforça junções, bordas e dobras do tecido. O rig deforma o tecido e articula as peças rígidas da armadura.

O Corrupted Grunt, a lâmina, o piso, o portão e o altar foram modelados pelo script `Tools/Blender/scripts/vertical_slice.py` e exportados para `Assets/_Game/Art/VerticalSlice`. O shader combina variação de pigmento em espaço local, luz difusa, sombra, reflexos de metal e realce frio nas bordas. O inimigo usa pivôs nas pernas e no braço dominante e reaproveita os tempos do combate.

| Modelo | Triângulos |
| --- | ---: |
| Fallen Knight | 8.728 |
| Corrupted Grunt | 6.328 |
| Espada | 136 |
| Piso completo | 7.288 |
| Portão | 1.624 |
| Altar da vigília | 492 |

O Blender 4.5.3 confirmou a contagem de triângulos na reimportação dos FBX. O Fallen Knight também passou pela verificação de pesos e de pintura por vértice no FBX reimportado. A Unity reconhece os 20 ossos do rig.

No Unity 6.3 LTS, o teste do encontro passou em headless e com a GPU Intel/Mesa. Ele verifica a articulação da coxa ao caminhar, o giro da espada durante o golpe e o acompanhamento da mão, além da esquiva, do dano e da progressão pela Lâmina de Cinza. Cada execução teve 1 teste aprovado e 0 falhas.

## Interface do encontro

A vida e o dano do jogador ficam no canto inferior. A vida do inimigo aparece sobre a criatura quando ela está ao alcance de detecção. O prompt de coleta acompanha a lâmina e o prompt de equipamento aparece junto do jogador. O painel de fim de encontro oferece uma única ação para recomeçar.
