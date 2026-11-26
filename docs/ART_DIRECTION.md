# Direção de arte

Dark fantasy low-poly. Ruínas de basalto, pedra marcada pelo tempo, troncos mortos e superfícies foscas definem a arena. Reserve cor mais saturada para silhuetas, efeitos e elementos interativos.

| Uso | Cor sugerida |
| --- | --- |
| Fundo e sombra | `#121719` |
| Basalto | `#252D2D` |
| Pedra de cinza | `#5B5145` |
| Madeira seca | `#392B20` |
| Aço do Fallen Knight | `#477A85` |
| Ferrugem da corrupção | `#A64026` |

- Teste as silhuetas no ângulo da câmera do jogo. A direção dos atores deve ser reconhecível pelo contorno ou detalhes de cor.
- Use faces planas, chanfros de um segmento quando úteis e materiais simples com pouca reflexão.
- Modele em metros, com ponto de apoio no chão. Use colisores simples na Unity e valide escala, normais, UVs e custo de renderização.
- O Fallen Knight tem placas sobrepostas, um elmo com proteção de nuca e queixo, manto assimétrico e uma fenda turquesa. O Corrupted Grunt é mais baixo, com chifres, núcleo incandescente e um antebraço dominante que se ergue antes do golpe.
- A Lâmina de Cinza tem uma geometria própria para o objeto caído e para a arma na mão. A lâmina equipada fica turquesa. As chamas do portão são pequenas para preservar o contraste do setor laranja de ataque no chão.
- O piso de lajes destaca a faixa central e leva ao portão de pedra. Nas bordas do pátio, o altar com cinzas, velas e tecido, as pedras soltas e as lajes faltantes mostram sinais da vigília funerária. O portão permanece fechado nesta etapa.

O desenho das passagens e a organização visual do pátio têm como referência as cenas de *Death’s Door*. A diferenciação de silhueta e material por personagem parte da direção pictórica de *No Rest for the Wicked* e de peças históricas das Royal Armouries. A iluminação usa uma base fria e acentos quentes localizados para separar o cenário da ação. [Demonstração da Acid Nerve](https://www.youtube.com/watch?v=pcSmBGkbd-g), [Moon Studios](https://norestforthewicked.com/news/introducing-no-rest-for-the-wicked), [Armadura histórica](https://royalarmouries.org/collection/object/object-3178), [Arte e iluminação de Diablo IV](https://news.blizzard.com/en-us/article/23964183/peeling-back-the-varnish-the-graphics-of-diablo-iv)

## Produção

Os dois personagens, a lâmina, o piso, o portão e o altar foram modelados pelo script `Tools/Blender/scripts/vertical_slice.py` e exportados para `Assets/_Game/Art/VerticalSlice`. O shader da arena usa luz difusa, sombra e um realce frio nas bordas das silhuetas. O movimento usa pivôs nas pernas e no braço dominante do inimigo e reaproveita os tempos do combate.

| Modelo | Triângulos |
| --- | ---: |
| Fallen Knight | 1.568 |
| Corrupted Grunt | 1.208 |
| Espada | 136 |
| Piso completo | 7.288 |
| Portão | 1.624 |
| Altar da vigília | 492 |

O Blender 4.5.3 confirmou a contagem de triângulos na reimportação dos seis FBX. As prévias da arena usam a mesma câmera e as mesmas posições dos personagens para comparar o acabamento.

No Unity 6.3 LTS, o teste do encontro passou em headless e com a GPU Intel/Mesa. Ele verificou o movimento do braço durante a preparação, a esquiva e a progressão pela Lâmina de Cinza, com 1 teste aprovado e 0 falhas em cada execução.
