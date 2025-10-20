import React, { useEffect, useState } from 'react';
import axios from 'axios';

export default function Clients({ token }) {
    const [list, setList] = useState([]);
    const [form, setForm] = useState({ cpfcnpj: '', name: '', email: '', phone: '', observations: '' });
    const [editing, setEditing] = useState(null);

    useEffect(() => {
        if (token) fetchClients();
    }, [token]);

    async function fetchClients() {
        try {
            const res = await axios.get('http://localhost:5000/clients', { headers: { Authorization: 'Bearer ' + token } });
            setList(res.data);
        } catch (e) {
            alert('Erro ao buscar clientes');
        }
    }

    async function handleSubmit(e) {
        e.preventDefault();
        try {
            if (editing) {
                await axios.put(`http://localhost:5000/clients/${editing}`, {
                    CPFCNPJ: form.cpfcnpj,
                    Name: form.name,
                    Email: form.email,
                    Phone: form.phone,
                    Observations: form.observations
                }, { headers: { Authorization: 'Bearer ' + token } });
                setEditing(null);
            } else {
                await axios.post('http://localhost:5000/clients', {
                    CPFCNPJ: form.cpfcnpj,
                    Name: form.name,
                    Email: form.email,
                    Phone: form.phone,
                    Observations: form.observations
                }, { headers: { Authorization: 'Bearer ' + token } });
            }
            setForm({ cpfcnpj: '', name: '', email: '', phone: '', observations: '' });
            fetchClients();
        } catch (e) {
            alert('Erro ao salvar cliente');
        }
    }

    async function handleDelete(cpfcnpj) {
        if (!confirm('Deseja realmente excluir este cliente?')) return;
        try {
            await axios.delete(`http://localhost:5000/clients/${cpfcnpj}`, { headers: { Authorization: 'Bearer ' + token } });
            fetchClients();
        } catch (e) {
            alert('Erro ao excluir cliente');
        }
    }

    function handleEdit(client) {
        setEditing(client.cpfcnpj);
        setForm({
            cpfcnpj: client.cpfcnpj,
            name: client.name,
            email: client.email || '',
            phone: client.phone || '',
            observations: client.observations || ''
        });
    }

    function handleCancel() {
        setEditing(null);
        setForm({ cpfcnpj: '', name: '', email: '', phone: '', observations: '' });
    }

    return (
        <div>
            <h2>Clientes</h2>
            <form onSubmit={handleSubmit} className="form-card">
                <input placeholder="CPF/CNPJ" value={form.cpfcnpj} onChange={e => setForm({ ...form, cpfcnpj: e.target.value })} disabled={editing} required />
                <input placeholder="Nome" value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} required />
                <input placeholder="Email" type="email" value={form.email} onChange={e => setForm({ ...form, email: e.target.value })} />
                <input placeholder="Telefone" value={form.phone} onChange={e => setForm({ ...form, phone: e.target.value })} />
                <textarea placeholder="Observações" value={form.observations} onChange={e => setForm({ ...form, observations: e.target.value })} />
                <div className="form-actions">
                    <button type="submit">{editing ? 'Atualizar' : 'Adicionar'}</button>
                    {editing && <button type="button" onClick={handleCancel}>Cancelar</button>}
                </div>
            </form>
            <table>
                <thead>
                    <tr>
                        <th>CPF/CNPJ</th>
                        <th>Nome</th>
                        <th>Email</th>
                        <th>Telefone</th>
                        <th>Ações</th>
                    </tr>
                </thead>
                <tbody>
                    {list.map(c => (
                        <tr key={c.cpfcnpj}>
                            <td>{c.cpfcnpj}</td>
                            <td>{c.name}</td>
                            <td>{c.email}</td>
                            <td>{c.phone}</td>
                            <td>
                                <button onClick={() => handleEdit(c)}>Editar</button>
                                <button onClick={() => handleDelete(c.cpfcnpj)}>Excluir</button>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}

