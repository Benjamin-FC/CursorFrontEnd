# Design Documentation

## Overview

The CRM Client Data Fetcher application follows a modern, clean design system based on the **Earth Design System** (`@frankcrum/earth`). The UI emphasizes clarity, usability, and visual consistency across all components.

## Design System

### Earth Design System Integration

The application uses the **Earth Design System** for consistent styling:

- **CSS Package**: `@frankcrum/earth` (local build from `packages/earth-local`)
- **Tailwind CSS**: v4.0.0 for utility classes
- **PostCSS**: For CSS processing with Tailwind plugin

### Color Palette

The application uses Earth design system CSS variables:

#### Brand Colors
- **Primary**: `#193567` (Deep Navy Blue)
- **Secondary**: `#1999d6` (Bright Blue)
- **Tertiary**: `#a3cf5e` (Lime Green)

#### Text Colors
- **Title**: `#3d4044` (Dark Gray)
- **Body**: `#404855` (Medium Gray)
- **Caption**: `#565c6a` (Light Gray)
- **Placeholder**: `#565c6a` (Light Gray)
- **Disabled**: `#bdc1c8` (Very Light Gray)

#### Surface Colors
- **Background**: `#fefefe` (Off-White)
- **Surfaces**: `#ffffff` (Pure White)
- **Tertiary**: `#f7f7f8` (Light Gray)
- **Hover**: `#e8eaed` (Light Blue-Gray)
- **Clicked**: `#f3f7fd` (Very Light Blue)

#### Border Colors
- **Default**: `#e0e2e6` (Light Gray)
- **Input**: `#898e9f` (Medium Gray)
- **Focused**: `#193567` (Brand Primary)
- **Danger**: `#ca1212` (Red)

## Component Design

### 1. Login Page (`Login.jsx`)

**Purpose**: User authentication entry point

**Design Features**:
- Centered card layout with backdrop
- Clean, minimal form design
- Icon-enhanced input fields (envelope for username, lock for password)
- Earth theme color scheme
- Smooth transitions and hover effects

**Key Styling**:
- Card with rounded corners (16px)
- Subtle shadow for depth
- Input fields with left-aligned icons
- Focus states with brand color accent
- Error messages with red accent border

**User Flow**:
1. User enters username and password
2. Form validates input (both fields required)
3. On success, redirects to main application
4. On failure, displays inline error message

### 2. Main Application Page (`App.jsx`)

**Purpose**: Client data search and display interface

**Design Features**:
- Unified card container design
- Animated gradient background
- Search form with icon-enhanced input
- Card-based data display
- Smooth animations and transitions

#### Header Section
- Fixed header with app title
- User welcome message with icon
- Logout button with hover effects
- Clean separation from main content

#### Search Section
- Card header with title and subtitle
- Search input with magnifying glass icon
- Search button with loading spinner state
- Inline error messages with icon

#### Results Section
- Appears inline within the same card
- Gradient background transition
- Results header with icon and clear button
- Data displayed in individual cards

#### Data Cards
Each data row is displayed as a card with:
- **Gradient accent border**: 4px left border with brand primary-to-secondary gradient
- **Hover effects**: Elevation, shadow enhancement, and background transition
- **Data key styling**: Bold text with decorative dot indicator
- **Data value containers**: Light background with border and rounded corners
- **Smooth animations**: Slide-in and hover transitions

**Visual Hierarchy**:
1. Card container (main-card)
2. Section headers (card-header, results-header)
3. Form elements (input, button)
4. Data cards (data-row)
5. Text content (data-value)

### 3. Protected Route Component (`ProtectedRoute.jsx`)

**Purpose**: Route guard for authenticated pages

**Behavior**:
- Checks authentication state on mount
- Shows loading state during check
- Redirects to login if not authenticated
- Preserves attempted location for post-login redirect

### 4. Authentication Context (`AuthContext.jsx`)

**Purpose**: Global authentication state management

**Features**:
- React Context API for state sharing
- localStorage persistence
- Loading state management
- Login/logout functions

**State Management**:
- `isAuthenticated`: Boolean authentication status
- `username`: Current user's username
- `isLoading`: Initial authentication check state

## Styling Patterns

### Background Design

**Animated Gradient Background**:
- Subtle animated gradient using Earth theme colors
- Smooth 15-second animation cycle
- Creates depth without distraction
- Uses `linear-gradient` with `background-position` animation

```css
background: linear-gradient(135deg, #f7f7f8 0%, #fefefe 50%, #f7f7f8 100%);
background-size: 200% 200%;
animation: gradientShift 15s ease infinite;
```

### Card Design

**Main Card Container**:
- White background with subtle shadow
- Rounded corners (16px)
- Border for definition
- Backdrop filter for modern glass effect

**Data Cards**:
- Individual cards for each data row
- Gradient accent border (4px left)
- Hover elevation effect
- Smooth transitions (0.3s cubic-bezier)

### Icon Integration

**Icon Usage**:
- Inline SVG icons for visual clarity
- Consistent sizing (16-20px)
- Color follows Earth theme
- Icons used for:
  - Search input (magnifying glass)
  - Loading state (spinner)
  - Error messages (alert circle)
  - Results header (users icon)
  - Clear button (X icon)
  - Username field (envelope)
  - Password field (lock)

### Typography

**Font Hierarchy**:
- **Page Title**: 24px, weight 600, letter-spacing -0.02em
- **Card Title**: 22px, weight 600, letter-spacing -0.01em
- **Section Title**: 16px, weight 600
- **Body Text**: 14-15px, weight 400
- **Labels**: 14px, weight 600

**Font Family**:
- System fonts for UI text
- Monospace (Monaco, Menlo, Courier New) for code/data display

### Spacing System

**Padding**:
- Card padding: 28-32px
- Section padding: 24-32px
- Input padding: 14px 16px (with 48px left for icon)
- Button padding: 14px 28px

**Gaps**:
- Form gaps: 16px
- Grid gaps: 12-16px
- Header gaps: 20px

**Margins**:
- Section margins: 24-32px
- Card margins: 0 (within container)

### Border Radius

- **Cards**: 12-16px
- **Inputs**: 8-10px
- **Buttons**: 8-10px
- **Data value containers**: 6px

### Shadows

**Shadow Hierarchy**:
- **Subtle**: `0 1px 3px rgba(0, 0, 0, 0.08)`
- **Medium**: `0 2px 8px rgba(0, 0, 0, 0.1)`
- **Elevated**: `0 4px 12px rgba(25, 53, 103, 0.15)`

### Transitions and Animations

**Transition Timing**:
- Standard: `0.2s ease`
- Smooth: `0.3s cubic-bezier(0.4, 0, 0.2, 1)`

**Animations**:
- **Slide In**: Fade and translate from top (-8px to 0)
- **Gradient Shift**: Background position animation (15s infinite)
- **Hover Effects**: Transform, shadow, and color transitions

## Responsive Design

### Breakpoints

**Mobile** (max-width: 768px):
- Reduced padding (20-24px)
- Stacked form layout
- Single column data rows
- Vertical header layout

### Mobile Optimizations

- Touch-friendly button sizes (min 44px)
- Adequate spacing for touch targets
- Readable font sizes
- Full-width form inputs

## Accessibility

### ARIA Labels

- Clear button: `aria-label="Clear results"`
- Form inputs: Proper `id` and `htmlFor` associations
- Error messages: Semantic HTML structure

### Keyboard Navigation

- Tab order follows visual flow
- Focus states clearly visible
- Enter key submits forms
- Escape key could clear results (future enhancement)

### Color Contrast

- All text meets WCAG AA contrast requirements
- Focus states use high-contrast borders
- Error messages use red with sufficient contrast

## User Experience Patterns

### Loading States

- **Search Button**: Shows spinner icon and "Searching..." text
- **Protected Route**: Shows "Loading..." during auth check
- **Button Disabled**: Visual opacity reduction (0.5)

### Error Handling

- **Inline Error Messages**: Displayed within form context
- **Error Styling**: Red border, background, and icon
- **User-Friendly Messages**: Clear, actionable error text

### Success States

- **Results Display**: Smooth slide-in animation
- **Visual Feedback**: Clear indication of successful data retrieval
- **Data Presentation**: Organized, scannable card layout

### Empty States

- **No Results**: Form ready for input
- **Cleared Results**: Smooth transition back to search state

## Design Principles

1. **Consistency**: All components follow Earth design system
2. **Clarity**: Clear visual hierarchy and information architecture
3. **Feedback**: Immediate visual feedback for user actions
4. **Accessibility**: WCAG AA compliant color contrast and keyboard navigation
5. **Performance**: Smooth animations without jank
6. **Responsiveness**: Works seamlessly across device sizes

## Future Design Enhancements

Potential improvements:
- Dark mode support
- Enhanced empty states
- Skeleton loading states
- Toast notifications for actions
- Enhanced data visualization
- Advanced search filters
- Data export functionality
- Keyboard shortcuts
