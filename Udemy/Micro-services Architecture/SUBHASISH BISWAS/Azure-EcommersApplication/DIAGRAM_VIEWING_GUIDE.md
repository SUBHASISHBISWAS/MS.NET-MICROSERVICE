# How to View the Sequence Diagrams

The **EVENT_SOURCING_GUIDE.md** file contains comprehensive sequence diagrams showing the complete flow of your microservices architecture with Event Sourcing.

## Viewing Options

### Option 1: GitHub (Best Experience) ⭐

**Upload to GitHub and the Mermaid diagrams will render automatically!**

1. Commit and push the file to GitHub
2. View EVENT_SOURCING_GUIDE.md on GitHub
3. All Mermaid diagrams will be beautifully rendered

### Option 2: VS Code with Extensions

**Install these VS Code extensions:**

1. **Markdown Preview Mermaid Support**
   ```bash
   code --install-extension bierner.markdown-mermaid
   ```

2. **Markdown All in One**
   ```bash
   code --install-extension yzhang.markdown-all-in-one
   ```

3. **Open the file and use preview:**
   - Open EVENT_SOURCING_GUIDE.md
   - Press `Ctrl+Shift+V` (Windows/Linux) or `Cmd+Shift+V` (Mac)
   - Mermaid diagrams will render in preview

### Option 3: Online Mermaid Editor

**For quick viewing:**

1. Go to: https://mermaid.live/
2. Copy a Mermaid diagram from the guide
3. Paste it into the editor
4. Diagram renders instantly
5. Can export as PNG/SVG

### Option 4: Convert to PDF

#### Using VS Code Extension

1. Install **Markdown PDF**:
   ```bash
   code --install-extension yzane.markdown-pdf
   ```

2. Open EVENT_SOURCING_GUIDE.md
3. Press `Ctrl+Shift+P` / `Cmd+Shift+P`
4. Type: "Markdown PDF: Export (PDF)"
5. Select export format
6. PDF with rendered diagrams is created!

#### Using Pandoc (Command Line)

**Install Pandoc:**
```bash
# macOS
brew install pandoc

# Windows (using Chocolatey)
choco install pandoc

# Linux (Ubuntu/Debian)
sudo apt-get install pandoc
```

**Convert to PDF:**
```bash
cd "/Users/subhasishbiswas/GIT/Interstellar/MS.NET-MICROSERVICE/Udemy/Micro-services Architecture/SUBHASISH BISWAS/Azure-EcommersApplication"

pandoc EVENT_SOURCING_GUIDE.md -o EVENT_SOURCING_GUIDE.pdf --pdf-engine=wkhtmltopdf
```

**Note:** For Mermaid diagrams in PDF, you might need additional setup. GitHub or VS Code preview is recommended.

#### Using Online Converters

1. **Markdown to PDF:**
   - https://www.markdowntopdf.com/
   - https://md2pdf.netlify.app/

2. **With Mermaid Support:**
   - https://github.com/simonhaenisch/md-to-pdf (CLI tool)

### Option 5: View ASCII Diagrams Only

If you can't render Mermaid diagrams, all key flows also have ASCII art versions that work in any text editor!

## Sequence Diagrams Included

The guide now includes 6 comprehensive sequence diagrams:

### 1. End-to-End: Basket Checkout to Order Creation
- Shows complete flow from user checkout
- Through Basket API, Discount gRPC, Azure Service Bus
- To Ordering API and CosmosDB Event Store

### 2. Basket Checkout Flow (Detailed)
- Cache-aside pattern with Redis
- gRPC discount calculation
- MassTransit/Azure Service Bus publishing

### 3. Order Creation with Event Sourcing (Detailed)
- Event consumption from Azure Service Bus
- OrderES aggregate creation
- Event raising and persistence
- Optimistic concurrency checking

### 4. Order Query Flow (Event Replay)
- Loading events from CosmosDB
- Event deserialization
- State reconstruction via event replay

### 5. Concurrent Update with Optimistic Locking
- Two users loading same order
- Optimistic locking in action
- Concurrency conflict handling

### 6. Complete System Integration
- High-level architecture view
- All microservices interaction
- Data flow through the system

## Tips

- **For presentations:** Export diagrams as PNG from Mermaid Live Editor
- **For documentation:** Use GitHub markdown rendering
- **For offline viewing:** Use VS Code with extensions
- **For printing:** Convert to PDF using Pandoc or VS Code extension

## Quick Preview Script

Save this as `preview.sh`:

```bash
#!/bin/bash
# Quick preview of EVENT_SOURCING_GUIDE.md

# Check if VS Code is installed
if command -v code &> /dev/null; then
    echo "Opening in VS Code..."
    code EVENT_SOURCING_GUIDE.md
    code --reuse-window --command markdown.showPreview
else
    echo "VS Code not found. Opening in default editor..."
    open EVENT_SOURCING_GUIDE.md
fi
```

Make executable:
```bash
chmod +x preview.sh
./preview.sh
```

---

**Recommended:** Push to GitHub for the best viewing experience! 🚀
