import React from 'react';
import { AuthProvider } from './contexts/AuthContext';
import { WorkspaceProvider } from './contexts/WorkspaceContext';
import { LanguageProvider } from './contexts/LanguageContext';
import { ThemeProvider } from './contexts/ThemeContext';
import { AppRouter } from './router/AppRouter';

export const App: React.FC = () => {
  return (
    <ThemeProvider>
      <LanguageProvider>
        <AuthProvider>
          <WorkspaceProvider>
            <AppRouter />
          </WorkspaceProvider>
        </AuthProvider>
      </LanguageProvider>
    </ThemeProvider>
  );
};
export default App;

