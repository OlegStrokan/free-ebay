"""
Root conftest for VectorIndexerWorker tests.

The local qdrant_client.py module shadows the pip qdrant_client package.
We pre-load the pip package into sys.modules under a safe alias so that
test utilities can reference it without triggering circular imports.
"""
import sys
from pathlib import Path

# Ensure the project root (parent of tests/) is importable
_project_root = str(Path(__file__).parent.parent)
if _project_root not in sys.path:
    sys.path.insert(0, _project_root)
