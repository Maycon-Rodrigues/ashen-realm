# Arquitetura

Os componentes em `Assets/_Game/Scripts` usam referências serializadas e parâmetros ajustáveis no Inspector. O gerador em `Assets/_Game/Editor` monta a cena e resolve as referências dos objetos.

| Componente | Responsabilidade |
| --- | --- |
| `PlayerController` | Lê WASD ou analógico, projeta o movimento nos eixos da câmera e move um CharacterController com gravidade. |
| `IsometricCamera` | Mantém pitch e yaw fixos e segue o jogador em LateUpdate com amortecimento. |
| `Health` | Inicializa e expõe HP, recebe dano e emite o evento de morte uma vez. |
| `PlayerCombat` | Lê ataque, registra a direção na preparação e consulta colliders no impacto, filtrando por layer e ângulo. |
| `EnemyAI` | Usa NavMeshAgent para perseguir, fixa a direção durante o aviso e verifica alcance e ângulo ao golpear. |
| `CombatFeedback` | Anima espada e corpo, desenha a área de perigo e toca os sons de golpe e impacto. |
| `SandboxProgression` | Controla a queda, a coleta por proximidade e o equipamento da Lâmina de Cinza. Aplica o bônus ao combate e troca o material da lâmina. |
| `SandboxStatus` | Mostra HP, perigo, vitória e derrota. Usa a cena registrada no build para reiniciar o encontro. |

A movimentação usa `Time.deltaTime`. O CharacterController aplica colisões sem exigir Rigidbody. Durante o golpe, o jogador pode andar mantendo a direção que escolheu ao atacar. Múltiplos colliders da mesma entidade compartilham uma referência de `Health` e recebem uma aplicação de dano por golpe. A apresentação usa o progresso da preparação e os eventos de impacto e de dano. O agente inimigo permanece parado durante a preparação e a recuperação, o que dá ao jogador tempo para recuar e contra-atacar.

O Editor cria a arena, registra a cena no perfil padrão, configura URP e Input System, e usa os colliders da arena como fonte da NavMesh. O repositório inclui a cena, a NavMesh, os materiais e as configurações inicializadas pelo Unity. O gerador permite recriar a arena caso a cena esteja ausente. As fontes C# e seus metadados também são versionados.
