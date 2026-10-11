"""Validate content references and provenance before importing/exporting an art update."""
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def read(relative):
    return json.loads((ROOT / relative).read_text(encoding='utf-8-sig'))


def main():
    manifest = read('assets/asset_manifest.json')
    entries = manifest['entries']
    index = {entry['key']: entry for entry in entries}
    errors = []
    if len(index) != len(entries): errors.append('Duplicate stable asset keys')
    for entry in entries:
        relative = entry['path'].removeprefix('res://')
        path = (ROOT / relative).resolve()
        if not entry['path'].startswith('res://assets/') or not path.is_relative_to(ROOT / 'assets'):
            errors.append(f"Invalid asset path: {entry['key']}"); continue
        if not path.is_file(): errors.append(f"Missing file: {relative}"); continue
        if hashlib.sha256(path.read_bytes()).hexdigest() != entry.get('sha256'):
            errors.append(f"Hash mismatch; rebuild index after a deliberate edit: {relative}")
    for monster in read('docs/design/09_level_monster_drop_sample.json')['monsters']:
        key = 'monster:' + monster['monster_id']
        if key not in index or index[key]['path'] != monster.get('visual', {}).get('portrait'):
            errors.append(f'Monster portrait and manifest disagree: {key}')
    for template in read('docs/design/12_equipment_sample_qi_refining.json')['equipment_templates']:
        key = 'equipment:' + template['equipment_template_id']
        if key not in index or index[key].get('icon_id') != template.get('icon_id'):
            errors.append(f'Equipment template missing artwork: {key}')
    source = (ROOT / 'scripts/ui/UiText.cs').read_text(encoding='utf-8')
    item_names = source.split('public static string BackpackItemName(string itemId)', 1)[1].split('public static string SlotLabel', 1)[0]
    for item in re.findall(r'"([a-z0-9_]+)"\s*=>', item_names):
        if 'item:' + item not in index: errors.append(f'Missing named inventory item: {item}')
    font = index['font:ui_sans_sc']['source']
    original = ROOT / font['font_file']
    if not original.is_file() or hashlib.sha256(original.read_bytes()).hexdigest() != font['font_file_sha256']:
        errors.append('Official font source file is missing or changed')
    if not (ROOT / font['license_path']).is_file(): errors.append('Font license is missing')
    if errors:
        raise SystemExit('\n'.join(errors))
    print(f'ASSET VALIDATION: {len(entries)} stable keys, paths and hashes; monster, equipment and inventory coverage consistent.')


if __name__ == '__main__':
    main()
