import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { setToken } from '../services/auth';
import { Container, Row, Col, Card, Button, Form, Alert } from 'react-bootstrap';
import api from "../services/api";

function RegisterPage() {
    const navigate = useNavigate();

    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [errorMsg, setErrorMsg] = useState('');
    const [successMsg, setSuccessMsg] = useState('');

    const handleRegister = async (e) => {
        e.preventDefault();
        setErrorMsg('');
        setSuccessMsg('');

        try {
            const ok = await api.authRegister(username, password)

            ok && setSuccessMsg('User created successfully!');

            const { token } = await api.authLogin(username, password)

            setToken(token);
            navigate('/dashboard');

        } catch (err) {
            setErrorMsg(err.response?.data || 'Something went wrong, Registration failed. Try again.');
        }
    };

    const goToLogin = () => {
        navigate('/login');
    };

    return (
        <Container className="d-flex flex-column justify-content-center align-items-center vh-100">
            <Row>
                <Col>
                    <h1 className="text-center mb-4">Create Account</h1>
                </Col>
            </Row>
            <Row>
                <Col>
                    <Card style={{ width: '400px', borderRadius: '10px' }} className="mx-auto p-4 shadow">
                        <Card.Body>
                            {errorMsg && (
                                <Alert variant="danger">{errorMsg}</Alert>
                            )}
                            {successMsg && (
                                <Alert variant="success">{successMsg}</Alert>
                            )}
                            <Form onSubmit={handleRegister}>
                                <Form.Group className="mb-3">
                                    <Form.Label>Username</Form.Label>
                                    <Form.Control
                                        type="text"
                                        value={username}
                                        onChange={(e) => setUsername(e.target.value)}
                                        placeholder="Choose a username"
                                    />
                                </Form.Group>
                                <Form.Group className="mb-3">
                                    <Form.Label>Password</Form.Label>
                                    <Form.Control
                                        type="password"
                                        value={password}
                                        onChange={(e) => setPassword(e.target.value)}
                                        placeholder="Enter a password"
                                    />
                                </Form.Group>
                                <div className="d-grid gap-2">
                                    <Button variant="success" type="submit">
                                        Sign Up
                                    </Button>
                                    <Button variant="outline-secondary" onClick={goToLogin}>
                                        Already have an account? Log In
                                    </Button>
                                </div>
                            </Form>
                        </Card.Body>
                    </Card>
                </Col>
            </Row>
        </Container>
    );
}

export default RegisterPage;
