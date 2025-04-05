import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {setToken} from '../services/auth';
import { Container, Row, Col, Card, Button, Form, Alert } from 'react-bootstrap';
import api from "../services/api.js";

function LoginPage() {
    const navigate = useNavigate();

    const [username, setUsername] = useState('');
    const [password, setPassword] = useState('');
    const [errorMsg, setErrorMsg] = useState('');

    const handleLogin = async (e) => {
        e.preventDefault();
        setErrorMsg('');

        try {
            const { token } = await api.authLogin(username, password)
            setToken(token); // store in localStorage
            navigate('/dashboard'); // go to Dashboard
        } catch (err) {
            setErrorMsg(err.response?.data || 'Invalid username or password');
        }
    };

    const goToRegister = () => {
        navigate('/register');
    };

    return (
        <Container className="d-flex flex-column justify-content-center align-items-center vh-100">
            <Row>
                <Col>
                    <h1 className="text-center mb-4">Blob Storage System</h1>
                </Col>
            </Row>
            <Row>
                <Col>
                    <Card style={{ width: '400px', borderRadius: '10px' }} className="mx-auto p-4 shadow">
                        <Card.Body>
                            {errorMsg && (
                                <Alert variant="danger">{errorMsg}</Alert>
                            )}
                            <Form onSubmit={handleLogin}>
                                <Form.Group className="mb-3">
                                    <Form.Label>Username</Form.Label>
                                    <Form.Control
                                        type="text"
                                        value={username}
                                        onChange={(e) => setUsername(e.target.value)}
                                        placeholder="Enter username"
                                    />
                                </Form.Group>
                                <Form.Group className="mb-3">
                                    <Form.Label>Password</Form.Label>
                                    <Form.Control
                                        type="password"
                                        value={password}
                                        onChange={(e) => setPassword(e.target.value)}
                                        placeholder="Enter password"
                                    />
                                </Form.Group>
                                <div className="d-grid gap-2">
                                    <Button variant="primary" type="submit">
                                        Login
                                    </Button>
                                    <Button variant="outline-secondary" onClick={goToRegister}>
                                        Sign Up
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

export default LoginPage;
