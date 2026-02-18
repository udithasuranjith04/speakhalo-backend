#!/bin/bash
# Quick script to open your User Secrets file in your editor

SECRETS_FILE="/Users/udithasuranjith/.microsoft/usersecrets/dcf84ea2-8086-4faf-9e1e-b9a1a709ed6a/secrets.json"

echo "Opening User Secrets file..."
echo "Location: $SECRETS_FILE"
echo ""

# Try different editors
if command -v code &> /dev/null; then
    # VS Code
    code "$SECRETS_FILE"
    echo "✅ Opened in VS Code"
elif command -v open &> /dev/null; then
    # macOS default editor
    open "$SECRETS_FILE"
    echo "✅ Opened in default editor"
else
    # Show path for manual opening
    echo "Please open this file manually:"
    echo "$SECRETS_FILE"
fi
