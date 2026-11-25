# Arquitetura

Os componentes em `Assets/_Game/Scripts` usam referências serializadas e parâmetros ajustáveis no Inspector. O gerador em `Assets/_Game/Editor` monta a cena e resolve as referências dos objetos.

| Componente | Responsabilidade |
| --- | --- |
| `PlayerController` | Lê WASD ou analógico, projeta o movimento nos eixos da câmera e move um CharacterController com gravidade. |
| `IsometricCamera` | Mantém pitch e yaw fixos e segue o jogador em LateUpdate com amortecimento. |
| `Health` | Inicializa e expõe HP, recebe dano e emite o evento de morte uma vez. |
| `PlayerCombat` | Lê ataque, controla cooldown, consulta colliders numa esfera e filtra por layer e ângulo. |
| `EnemyAI` | Usa NavMeshAgent para perseguir, controla a rotação e causa dano por distância e cooldown. |
| `SandboxStatus` | Mostra HP e controles durante o protótipo. |

A movimentação usa `Time.deltaTime`. O CharacterController aplica colisões sem exigir Rigidbody. No ataque, múltiplos colliders da mesma entidade compartilham uma referência de `Health` e recebem uma aplicação de dano por golpe. O evento `Attacked` pode acionar animação ou efeitos quando esses sistemas forem adicionados. A IA para o agente ao entrar em alcance, gira na direção do alvo e desativa o ator ao morrer.

O Editor cria a arena, registra a cena no perfil padrão, configura URP e Input System, e usa os colliders da arena como fonte da NavMesh. Os arquivos internos e a cena inicial são gerados pelo Unity, e permanecem como saídas locais enquanto a primeira abertura estiver pendente. As fontes C# e suas metas são versionadas.
