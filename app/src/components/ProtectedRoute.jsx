import { Navigate } from 'react-router-dom';
import { isAuthenticated } from '../services/auth';

// eslint-disable-next-line react/prop-types
function ProtectedRoute({ children }) {
    if (!isAuthenticated()) {
        return <Navigate to="/login" replace />;
    }
    return children;
}

export default ProtectedRoute;
