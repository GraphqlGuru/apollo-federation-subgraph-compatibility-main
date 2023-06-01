package com.example.demo

import com.example.demo.model.Product
import com.expediagroup.graphql.generator.scalars.ID
import com.expediagroup.graphql.server.operations.Query
import org.springframework.stereotype.Component

@Component
class ProductQuery : Query {
    fun product(id: ID) = Product.byID(id)
}
