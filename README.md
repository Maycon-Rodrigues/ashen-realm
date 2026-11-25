# Ashen Realm

Action RPG 3D isométrico para PC, single-player, com estética dark fantasy low-poly. O Sprint 01 é uma arena para experimentar aproximação, ataque corpo a corpo e a reação de um inimigo.

## Pré-requisitos

- Unity 6.0 LTS (6000.0), Editor com licença ativa e acesso ao registro de pacotes. O projeto usa C#, URP, Input System e AI Navigation.
- Git e Bash. Blender 4.5 LTS é opcional para jogar e necessário para gerar os modelos procedurais.

O ambiente de desenvolvimento é Omarchy 4.5.0 x86_64. A Unity lista Ubuntu 22.04/24.04 entre os sistemas Linux suportados pelo Editor. A compatibilidade com Omarchy e Hyprland ainda precisa ser testada. Uma estação com um sistema suportado pode executar o mesmo bootstrap. [Requisitos oficiais do Unity 6.0](https://docs.unity3d.com/6000.0/Documentation/Manual/system-requirements.html)

## Gerar e jogar

Na raiz do clone, indique o executável instalado. O bootstrap usa o Editor para criar as configurações iniciais do projeto, resolver os pacotes e gerar a arena com a NavMesh já configurada. Feche qualquer Editor que esteja usando o mesmo projeto durante o bootstrap.

```bash
export UNITY_EDITOR="/caminho/para/Editor/Unity"
./Tools/Unity/bootstrap.sh
"$UNITY_EDITOR" -projectPath "$PWD" -openfile Assets/_Game/Generated/CombatSandbox.unity
```

Pressione **Play**. Mova-se com **WASD** ou o **analógico esquerdo**. Vire o personagem na direção do inimigo e ataque com **clique esquerdo** ou o **botão oeste** do gamepad. O HUD mostra os pontos de vida. Para reiniciar, saia e volte ao Play Mode.

No Editor, o menu **Ashen Realm → Open or Generate Combat Sandbox** reabre a cena existente ou cria a cena se ela estiver ausente. O checkout contém a fonte do gerador, com a cena e as configurações geradas localmente.

## Gerar modelos

```bash
blender --background --python-exit-code 1 \
  --python Tools/Blender/scripts/environment.py -- generate \
  --output-dir Assets/_Game/Art/Generated --verify
```

O Blender portátil 4.5.3 foi executado com sucesso no Omarchy. A geração da cena, a compilação e o Play Mode aguardam um Unity ativo. Consulte os [critérios do sprint](docs/SPRINT_01.md), a [arquitetura](docs/ARCHITECTURE.md) e o [pipeline de assets](docs/ASSET_PIPELINE.md).
