# Sprint 03 — Primeira progressão

A primeira vitória faz o Corrupted Grunt soltar a Lâmina de Cinza. Aproxime-se e pressione **E** ou o **botão sul** do gamepad para coletar. Em seguida pressione **Q** ou o **botão norte** para equipar.

Com a espada equipada, o dano sobe de 20 para 25. Na rodada seguinte, o Grunt ainda começa com 45 HP, e passa a cair em dois acertos. **R** ou **Start** inicia outra rodada após vitória ou derrota. A lâmina coletada acompanha essas reinicializações durante a sessão de jogo. Parar o Play Mode inicia uma nova sessão.

O HUD informa o dano e a próxima ação. A lâmina caída brilha em turquesa, e a espada muda de cor quando equipada.

## Verificação

O teste de Play Mode percorre esquiva e combate, coleta por proximidade, equipamento e reinício. Ele verifica três acertos com a arma inicial, dois com a Lâmina de Cinza e preservação do bônus após reiniciar depois de vitória ou derrota.

No Omarchy, o Unity 6.3 LTS registrou 1 teste aprovado e 0 falhas em cada execução, em headless e com a GPU Intel/Mesa. Os resultados estão em `Logs/sprint03-playmode.xml` e `Logs/sprint03-playmode-graphics.xml`. O teste usa dispositivos virtuais.
