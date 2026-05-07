# Issue Tracker

Issues for this repo live in **GitHub Issues** at [michaelvolz/redmuffin.Blazor.StaticWeb](https://github.com/michaelvolz/redmuffin.Blazor.StaticWeb).

Skills that read from or write to the issue tracker (`to-issues`, `triage`, `to-prd`, `qa`) use the `gh` CLI.

- Create: `gh issue create --title "..." --body "..."`
- List: `gh issue list --label "needs-triage" --json number,title,labels`
- View: `gh issue view <number> --json number,title,body,labels,comments`
- Comment: `gh issue comment <number> --body "..."`
- Close: `gh issue close <number>`
- Label: `gh issue edit <number> --add-label "ready-for-agent" --remove-label "needs-triage"`
