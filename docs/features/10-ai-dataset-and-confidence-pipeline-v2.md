# Feature 10 - AI Dataset And Confidence Pipeline v2

## Goal

Turn the current ML.NET scoring capability into a disciplined, explainable, and reproducible subsystem.

## Why This Needs A Dedicated Feature

The current AI flow is a promising prototype, but it is not yet rigorous enough for a strong dissertation section. Data collection, labeling, evaluation, and inference consistency need to be formalized.

## Scope

- define the canonical training dataset location
- separate unlabeled export rows from training-ready labeled data
- add dataset validation before training
- improve feature extraction consistency between export, training, and inference
- report evaluation metrics in a stable way
- persist model metadata such as training date and dataset size

## Recommended Changes

- split data into:
  - `training-candidates.csv`
  - `training-labeled.csv`
- reject invalid label values during training
- ensure console output, JSON, and SARIF all reflect post-AI confidence
- document the human labeling workflow

## Suggested Additional Features

- per-rule precision/recall tracking
- dataset class-balance reporting
- reproducible train/test split configuration
- optional model versioning metadata

## Dependencies

- Feature 02 tests
- Feature 09 reporting consistency is helpful

## Acceptance Criteria

- training fails fast on invalid dataset format
- labeled and unlabeled data are clearly separated
- inference uses the same feature definitions as training
- model evaluation output is reproducible and documented

## Dissertation Value

This feature makes the AI component defendable as a measured confidence layer rather than a vague claim of "AI-powered security."
