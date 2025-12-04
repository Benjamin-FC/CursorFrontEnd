# Client-Side Tests

This directory contains comprehensive test suites for the React frontend application.

## Test Setup

The project uses:
- **Vitest**: Fast unit test framework
- **React Testing Library**: Component testing utilities
- **jsdom**: DOM environment for testing
- **@testing-library/user-event**: User interaction simulation

## Running Tests

```bash
# Run tests in watch mode
npm test

# Run tests once
npm run test:run

# Run tests with UI
npm run test:ui

# Run tests with coverage
npm run test:coverage
```

## Test Structure

### Unit Tests

- **`contexts/__tests__/AuthContext.test.jsx`**: Tests for authentication context
  - Provider initialization
  - Login/logout functionality
  - State persistence
  - Error handling

- **`__tests__/Login.test.jsx`**: Tests for login component
  - Form rendering
  - User interactions
  - Form validation
  - Error handling
  - Accessibility

- **`__tests__/App.test.jsx`**: Tests for main application component
  - Search functionality
  - API integration
  - Data display
  - Error handling
  - Logout functionality

- **`__tests__/ProtectedRoute.test.jsx`**: Tests for route protection
  - Authentication checks
  - Redirect behavior
  - Loading states

### Integration Tests

- **`__tests__/integration.test.jsx`**: End-to-end user flows
  - Full authentication flow
  - Search flow after login
  - Logout flow
  - Error recovery
  - State persistence

## Test Utilities

### `test/utils.jsx`

Helper functions for testing:
- `renderWithProviders`: Render components with all providers
- `createMockResponse`: Create mock fetch responses
- `mockFetch`: Mock global fetch function
- `waitForAsync`: Wait for async operations

## Test Coverage

The test suite covers:
- ✅ Component rendering
- ✅ User interactions
- ✅ Form validation
- ✅ API integration
- ✅ Error handling
- ✅ Authentication flow
- ✅ Route protection
- ✅ State management
- ✅ Accessibility
- ✅ Edge cases

## Writing New Tests

When adding new tests:

1. Place component tests in `__tests__/ComponentName.test.jsx`
2. Place context tests in `contexts/__tests__/ContextName.test.jsx`
3. Use `renderWithProviders` for components that need routing/auth
4. Mock external dependencies (fetch, navigate, etc.)
5. Clean up after each test (handled automatically)
6. Test user interactions with `@testing-library/user-event`
7. Use `waitFor` for async operations

## Example Test

```jsx
import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import MyComponent from '../MyComponent.jsx'

describe('MyComponent', () => {
  it('should handle user interaction', async () => {
    const user = userEvent.setup()
    render(<MyComponent />)
    
    const button = screen.getByRole('button')
    await user.click(button)
    
    expect(screen.getByText('Clicked')).toBeInTheDocument()
  })
})
```
