# Ashen Realm

Action RPG 3D isométrico para PC, single-player, com estética dark fantasy low-poly. O CombatSandbox é uma arena para testar aproximação, esquiva e contra-ataque contra um inimigo.

## Pré-requisitos

- Unity 6.3 LTS (6000.3.29f1), Editor com licença ativa e acesso ao registro de pacotes. O projeto usa C#, URP, Input System e AI Navigation.
- Git e Bash. Blender 4.5 LTS é opcional para jogar e necessário para gerar os modelos procedurais.

O ambiente de desenvolvimento é Omarchy 4.5.0 x86_64. O Unity 6.3 LTS executou o bootstrap e a importação do FBX nesse ambiente com licença Personal. Consulte a lista de distribuições suportadas para outras estações Linux. [Requisitos oficiais do Unity 6.3](https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html)

## Gerar e jogar

No Unity Hub, adicione a raiz do clone e abra `Assets/_Game/Generated/CombatSandbox.unity`. Também é possível abrir pelo terminal.

```bash
export UNITY_EDITOR="/caminho/para/Editor/Unity"
"$UNITY_EDITOR" -projectPath "$PWD" \
  -executeMethod AshenRealm.Editor.CombatSandboxBuilder.Generate
```

Pressione **Play**. Mova-se com **WASD** ou o **analógico esquerdo**. Vire o personagem na direção do inimigo e ataque com **clique esquerdo** ou o **botão oeste** do gamepad. Saia da área laranja quando o inimigo preparar o golpe. Depois de vencer ou perder, pressione **R** ou **Start** para tentar de novo.

O checkout inclui a cena, a NavMesh, as configurações do Unity e o lock dos pacotes. No Editor, o menu **Ashen Realm → Open or Generate Combat Sandbox** reabre a cena ou cria uma nova se ela estiver ausente. Com o Editor gráfico fechado, `./Tools/Unity/bootstrap.sh` executa o mesmo fluxo pelo terminal.

## Gerar modelos

```bash
blender --background --python-exit-code 1 \
  --python Tools/Blender/scripts/environment.py -- generate \
  --output-dir Assets/_Game/Art/Generated --verify
```

O Blender portátil 4.5.3 foi executado com sucesso no Omarchy. Consulte os [resultados do Sprint 02](docs/SPRINT_02.md), a [arquitetura](docs/ARCHITECTURE.md) e o [pipeline de assets](docs/ASSET_PIPELINE.md).
