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
- O Fallen Knight tem armadura azul-escura, escudo, manto e um visor turquesa voltado para a frente. O Corrupted Grunt é mais largo e inclinado, com chifres, garras e um núcleo incandescente.
- A Lâmina de Cinza tem uma geometria própria para o objeto caído e para a arma na mão. A lâmina equipada fica turquesa. As chamas do portão são pequenas para preservar o contraste do setor laranja de ataque no chão.
- O piso de lajes destaca a faixa central e leva ao portão de pedra. O portão está fechado nesta etapa e marca a direção visual da futura expedição.

## Produção

Os dois personagens, a lâmina, o piso e o portão foram modelados pelo script `Tools/Blender/scripts/vertical_slice.py` e exportados para `Assets/_Game/Art/VerticalSlice`. O Unity aplica a paleta URP e configura as luzes da arena em espaço de cor linear. O movimento das pernas usa pivôs procedurais, e as poses de ataque reaproveitam os tempos do combate.

| Modelo | Triângulos |
| --- | ---: |
| Fallen Knight | 1.224 |
| Corrupted Grunt | 1.384 |
| Espada | 136 |
| Piso completo | 7.920 |
| Portão | 1.624 |

O Blender 4.5.3 confirmou a contagem de triângulos na reimportação dos cinco FBX. O Unity 6.3 LTS renderizou a arena e executou o teste de combate e progressão em headless e com a GPU Intel/Mesa, com 1 teste aprovado e 0 falhas em cada execução.
