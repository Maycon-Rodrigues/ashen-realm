# Sprint 02 — Combate legível

O CombatSandbox permite ler a preparação do golpe, recuar e contra-atacar no mesmo encontro. O jogador ainda vence o Corrupted Grunt com três acertos.

- A espada tem uma preparação de 0,17 s. O dano é calculado no impacto, usando o alcance do jogador e a direção registrada ao iniciar o ataque.
- O Corrupted Grunt avisa o golpe por 0,65 s com um setor laranja no chão. O setor corresponde ao alcance e ao ângulo do dano. A direção fica fixa até o golpe, seguido de 1,15 s de recuperação.
- O HUD mostra vitória ou derrota. **R**, **Start** e o botão na tela reiniciam o encontro.
- A arena usa rocha, árvore seca e altar procedurais. Os sons de balanço da arma e de acerto foram sintetizados para o protótipo.

## Verificação

O teste de Play Mode carrega a cena versionada, usa teclado, mouse e gamepad virtuais e verifica aviso, esquiva, dano sofrido, impacto atrasado, cooldown, dano único com múltiplos colliders, morte em três acertos e reinício após vitória e derrota. Execute o teste com o comando no [Sprint 01](SPRINT_01.md).

Em 25/11/2026, o Unity 6000.3.29f1 registrou 1 teste aprovado e 0 falhas tanto em headless quanto com a GPU Intel/Mesa no Omarchy. Os resultados locais estão em `Logs/sprint02-playmode.xml` e `Logs/sprint02-playmode-graphics.xml`.

Os três modelos foram exportados e reimportados no Blender 4.5.3. As variantes de rocha, árvore e altar têm 80, 144 e 132 triângulos, respectivamente. O Unity importou os FBX e instanciou os três modelos na cena durante a geração.

Os tempos e volumes são provisórios. A leitura visual e o áudio durante jogo manual, assim como um gamepad físico, ainda aguardam validação.
