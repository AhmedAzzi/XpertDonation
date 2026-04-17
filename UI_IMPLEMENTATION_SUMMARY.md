# XpertPharm5 UI Recreation Guide

## Overview
This document outlines the WPF UI components recreated to match the XpertPharm5 pharmacy management system interface. All views are built using MVVM-compatible patterns with centralized styling via ResourceDictionary.

---

## Created Views

### 1. **SalesCounterView** (`Views/SalesCounterView.xaml`)
**Purpose:** Point-of-sale transaction interface for dispensing medications

**Layout Structure:**
- **2-Column Grid Layout**
  - Left Panel (Primary): Product entry form + transaction table
  - Right Panel (Info Display): Product details, pricing summary

**Key Components:**
- **Form Inputs:**
  - Barcode textbox (F3 shortcut reference)
  - Price display (read-only)
  - Product dropdown selector (F4 reference)
  - Quantity input field

- **Data Table:**
  - 7 columns: PRODUIT, PRIX/U, Qté, Date Exp., Lot, TOTAL TTC, Code barre
  - Auto-generated columns binding to ViewModel
  - 280px height with scrollbar support

- **Info Panel (Dark Background - #0A1628):**
  - Product name display (green text - #22C55E)
  - Info grid with:
    - Lot number & Quantity
    - Reference & Location
    - Expiration date & Batch code
    - Tariff reference
  - Summary section:
    - Amount to pay (large green text - 22px)
    - Tariff breakdown (100%, 80%)
    - Medical, Other, Count fields

**Color Palette:**
- Dark panel background: #0A1628
- Green accent: #22C55E
- Info text: #94A3B8
- Amount text: #22C55E (bright green)

**Bindings Required:**
- ProductsGrid: IEnumerable<ProductViewModel>
- Info fields: Selected product details

---

### 2. **SalesJournalView** (`Views/SalesJournalView.xaml`)
**Purpose:** Transaction history and detailed sales reporting

**Layout Structure:**
- **3-Row Grid Layout**
  - Row 1: Filter panel (auto-height)
  - Row 2: Main transaction table + detail section
  - Row 3: Action buttons footer

**Key Components:**

- **Filter Panel (Top):**
  - Date range filters (Début, Fin)
  - Client dropdown
  - Saïsie Par dropdown
  - Date Éch dropdown
  - Search textbox
  - Filter options:
    - Checkbox: "Inclure les échanges annés"
    - Radio buttons: Tous, Payées, Impayées (Tous selected by default)

- **Main DataGrid (15 columns):**
  - N° Vente, Type Vente, Mont. Vente, Motif, Date
  - Clients, Remise globale, Mont. Vente (remisé)
  - Mont. Payé, Mont. Reste, Date Éche., Saisie Par
  - Saisie le, Nom Machine, Avec

- **Detail Section (Bottom):**
  - "Détail de la vente" header
  - Sub-table with 8 columns:
    - Code, Produit, Qte, Lot, Date préemp.
    - Code Barre Lot, Prix Vente, Mnt. Vente
  - Height: 140px (8 rows visible)

- **Footer Buttons:**
  - Nouveau[F10] - Accent blue
  - Supprimer [F3] - Red danger
  - Journal Détaillé - Orange warning
  - Text label: "Incluie les échanges annés"

**Header Styling:**
- TableHeaderBrush (#1A3A6B)
- White text, bold Segoe UI Semibold, 11px
- 36px height, 8px padding

**Bindings Required:**
- SalesGrid: IEnumerable<SaleViewModel>
- Detail grid: Selected sale line items

---

### 3. **DashboardView** (`Views/DashboardView.xaml`)
**Purpose:** Main home screen with quick access tiles and KPI indicators

**Layout Structure:**
- **2-Column Layout**
  - Left: Scrollable tile grid
  - Right: Fixed indicator panel

**Key Components:**

- **Tile Grid (2 columns, UniformGrid):**
  - 8 colored tiles (160px height, 10px spacing)
  - Each tile contains: Icon emoji + Title text
  - Tiles with hover effects (darker shade)

  **Tile Colors & Functions:**
  1. Vente Comptoir - Yellow (#FCD34D → #FBBF24 on hover)
  2. Journal Ventes - Green (#4ADE80 → #22C55E on hover)
  3. Entrées Stock - Red (#F87171 → #EF4444 on hover)
  4. Assistant Commandes - Lime (#84CC16 → #65A30D on hover)
  5. Liste des Users - Teal (#34D399 → #10B981 on hover)
  6. Journal Encaissements - Purple (#A78BFA → #9333EA on hover)
  7. Produits - Dark (#1F2937 → #374151 on hover)
  8. Hanquants - Red (#DC2626 → #B91C1C on hover)

- **Indicator Panel (Right):**
  - Card-based layout with colored backgrounds
  - 6 indicator cards:
    1. Manquants - Light blue (#DBEAFE) - 0
    2. Liste des manquants - Indigo (#E0E7FF) - 1857
    3. Lots périmés - Red (#FEE2E2) - 0
    4. Lots périmés 30j - Amber (#FEF3C7) - 15
    5. Lots périmés 60j - Amber (#FEF3C7) - 45
    6. Lots périmés 90j - Amber (#FEF3C7) - 65
    7. Règlement fournisseur - Red (#FEE2E2) - 0

  - Card styling:
    - CornerRadius: 6px
    - Padding: 12px
    - Icons: Emoji characters
    - Headers: 12px Semibold
    - Values: 18px Semibold colored text

- **Support Section (Bottom):**
  - Technical support phone numbers
  - Semibold title, secondary text color

**Tile ControlTemplate:**
- Shadow effect: DropShadow (BlurRadius=12, Opacity=0.15)
- CornerRadius: 8px
- Content centered vertically/horizontally
- White text on colored backgrounds (except yellow tile)

**Bindings Required:**
- Individual indicator values can be bound to ICommand properties
- Tiles can navigate to respective views via ViewModel

---

### 4. **ReceptionDocumentView** (`Views/ReceptionDocumentView.xaml`)
**Purpose:** Stock reception/goods receipt form with detailed product tracking

**Layout Structure:**
- **3-Row Grid Layout**
  - Row 1: Header info panel (auto-height)
  - Row 2: Scrollable product table section
  - Row 3: Summary footer with totals

**Key Components:**

- **Header Section:**
  - Title: "Bon de Réception N° 00428/20"
  - Form Grid 1:
    - N° Réception (textbox)
    - Fournisseur (textbox)
    - Date (textbox) - "07/04/2025"
    - Magasin (combobox) - "PHARMACIE"
    - Type T.T.C (combobox)
  
  - Form Grid 2:
    - N°BL, Num (textbox)
    - Num Éch (textbox)
    - Échéance (textbox)
    - Saisie (textbox)
  
  - Form Grid 3:
    - Solde (read-only) - "-38 980. 10"

  - Fields height: 28px, padding: 8,2
  - Light gray background for read-only fields

- **Products Section:**
  - Info row: N° Qté Reçue (0), Qté U.G % (0)
  - Column headers (dark blue background):
    - Code Barre, Produit, Produit (Full)
    - Qté Reçue, Qté U.G, Date péremption
    - Prix unitaire, SHP, PPA, Taux TVA
    - Montant H.T, Code Barre Lot
  
  - Data row (white background with border):
    - Sample data populated for demonstration
    - Numeric fields right-aligned
    - Product names wrapped

- **Footer Summary:**
  - "Nouveau" button (accent color)
  - Summary metrics in 2 rows, 3 columns:
    - Row 1: Total PPA (72,000.00), Total SHP (0.00), Total H.T (60,000.00)
    - Row 2: Total TVA (0.00), Timbre (0.00), Total T.T.C (60,000.00)
  - Totals: Right-aligned, bold values
  - Red color for critical amounts (#DC2626)

**Header Info Typography:**
- Labels: 11px, secondary gray
- Textbox: 12px, Segoe UI
- Titles: 13px Semibold

**Bindings Required:**
- Header fields: Reception document properties
- Product table: IEnumerable<ReceptionLineViewModel>
- Summary totals: Computed/aggregated properties
- Comboboxes: Reference data (suppliers, warehouses)

---

## Styling Architecture

### ResourceDictionary Location
All styles are defined in `Themes/Theme.xaml` for consistency.

### Color System
**Primary Colors:**
- Header: #0A1E3D, #1E3A5F
- Accent: #1D6CB5 (hover: #1557A0)
- Success: #15803D, #22C55E
- Danger: #DC2626 (hover: #B91C1C)
- Warning: #D97706
- Info Background: #0A1628

**Text Colors:**
- Primary: #1E293B
- Secondary: #64748B
- Slate Label: #94A3B8
- White: #FFFFFF

**Backgrounds:**
- Workspace: #EEF1F6
- Card: #FFFFFF
- Table Headers: #1A3A6B
- Table Alt Rows: #F8FAFC
- Selection: #DBEAFE

### Typography
**Body Text (LabelStyle):**
- Font: Segoe UI
- Size: 12px
- Color: TextSecondaryBrush (#64748B)
- V-Align: Center

**Value Text (ValueStyle):**
- Font: Segoe UI Semibold
- Size: 13px
- Color: TextPrimaryBrush (#1E293B)
- V-Align: Center

**Input Style:**
- Font: Segoe UI, 13px
- Height: 32px
- Padding: 8,4
- Border: 1px, CornerRadius: 4px
- Focus state: Accent color, BorderThickness: 2

---

## Implementation Notes

### MVVM Binding Patterns
```xaml
<!-- Example DataGrid Binding -->
<DataGrid ItemsSource="{Binding Items}"
          SelectedItem="{Binding SelectedItem}"
          AutoGenerateColumns="False">
    <DataGrid.Columns>
        <DataGridTextColumn Header="Name" 
                          Binding="{Binding PropertyName}"/>
    </DataGrid.Columns>
</DataGrid>
```

### Custom ControlTemplates
- Tile buttons use custom ControlTemplate with DropShadowEffect
- Text colors adapt to background (white on dark, dark on light)
- Hover states implemented via triggers

### Layout Best Practices
- Use Grid with ColumnDefinitions for precise control
- Use StackPanel for sequential content
- Avoid absolute positioning (Top, Left properties)
- Set MinWidth/MaxWidth on containers for responsive behavior
- Use Margin for spacing, Padding for internal content alignment

---

## Required ViewModel Properties

### SalesCounterViewModel
- `IEnumerable<CartItem> Products` - Observable collection
- `CartItem SelectedProduct` - Currently selected item
- `decimal TotalAmount` - Calculated total

### SalesJournalViewModel
- `IEnumerable<Sale> Sales` - Transaction list
- `Sale SelectedSale` - Selected transaction
- `ObservableCollection<SaleLine> SaleDetails` - Line items

### DashboardViewModel
- `int MissingCount` - Missing items count
- `int MissingListCount` - Missing list items
- `int ExpiredLotsCount` - Expired lots
- `ICommand NavigateToSalesCounter` - Navigation command

### ReceptionDocumentViewModel
- `Reception CurrentReception` - Document data
- `ObservableCollection<ReceptionLine> Lines` - Product lines
- `decimal TotalAmount` - Calculated total

---

## Future Enhancements

1. **Animations:**
   - Tile hover animations (ScaleTransform)
   - Smooth transitions between states

2. **Responsive Design:**
   - Adjust tile grid columns for different resolutions
   - Collapsible info panel on smaller screens

3. **Custom Controls:**
   - DatePicker with calendar popup
   - Auto-complete dropdown for products
   - Numeric input validation

4. **Accessibility:**
   - TabIndex values for form navigation
   - ARIA labels and descriptions
   - Keyboard shortcuts (F3, F4, F10 references already in UI)

---

## Testing Checklist

- [ ] All DataGrids populate with test data
- [ ] Filter controls update table properly
- [ ] Button click handlers work
- [ ] Tile navigation functions
- [ ] Responsive layout on different screen sizes
- [ ] Colors match specification exactly
- [ ] Typography sizing matches images
- [ ] Spacing and alignment precise to pixel
- [ ] Hover effects work smoothly
- [ ] No horizontal scrolling on standard resolutions

---

## File Summary

| File | Type | Purpose |
|------|------|---------|
| SalesCounterView.xaml | View | Point-of-sale interface |
| SalesCounterView.xaml.cs | Code-behind | Initialization |
| SalesJournalView.xaml | View | Transaction history |
| SalesJournalView.xaml.cs | Code-behind | Initialization |
| DashboardView.xaml | View | Main dashboard |
| DashboardView.xaml.cs | Code-behind | Initialization |
| ReceptionDocumentView.xaml | View | Stock reception form |
| ReceptionDocumentView.xaml.cs | Code-behind | Initialization |
| Themes/Theme.xaml | ResourceDictionary | Centralized styling |

---

## Integration Steps

1. Copy all `.xaml` and `.xaml.cs` files to respective folders
2. Update `App.xaml.csproj` to reference new views
3. Create corresponding ViewModels in `ViewModels/` folder
4. Bind views to ViewModels in MainWindow or NavigationViewModel
5. Implement navigation logic for tile clicks
6. Test with sample data before connecting to database

