# Automação Blender

Execute a partir da raiz do repositório usando Blender 4.5 LTS.

```bash
blender --background --python-exit-code 1 \
  --python Tools/Blender/scripts/environment.py -- generate --help
```

Use `import --help` para ver as opções de normalização de uma malha externa. O [pipeline de assets](../../docs/ASSET_PIPELINE.md) descreve os formatos, a validação e a importação no Unity.
