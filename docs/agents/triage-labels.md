# Triage Labels

The canonical triage state machine uses these label strings:

| Role            | GitHub Label      | Meaning                                        |
| --------------- | ----------------- | ---------------------------------------------- |
| Unlabeled       | _(no label)_      | Never triaged                                  |
| Needs Triage    | `needs-triage`    | Maintainer needs to evaluate                   |
| Needs Info      | `needs-info`      | Waiting on reporter for more information       |
| Ready for Agent | `ready-for-agent` | Fully specified — an AFK agent can pick it up  |
| Ready for Human | `ready-for-human` | Needs human implementation (judgment required) |
| Won't Fix       | `wontfix`         | Will not be actioned                           |

Categories (applied alongside a state label):

| Category | GitHub Label  |
| -------- | ------------- |
| Bug      | `bug`         |
| Feature  | `enhancement` |

Every triaged issue must carry exactly one state label and exactly one category label.
